'Imports System.IO
'Imports System.Net
'Imports System.Text
Imports System.Threading
Imports uPLibrary.Networking.M2Mqtt
Imports uPLibrary.Networking.M2Mqtt.Messages
'Imports HomeSeer.PluginSdk
'Imports HomeSeer.PluginSdk.Logging
'Imports HomeSeer.PluginSdk.Speech
'Imports HSCF.Communication.Scs.Communication.EndPoints.Tcp
'Imports HSCF.Communication.ScsServices.Client
'Imports System
'Imports System.Collections.Generic
'Imports System.Linq
'Imports System.Threading.Tasks
'Imports HomeSeer.PluginSdk.Constants


Public Class MessagingThread
    Public Shared TransmitMessageQueue As New Queue
    Public DevicePollTimerInterval As Integer
    Public MQTT_RecvTopic As String = "homeseer/evc/in"
    Public MQTT_SendTopic As String = "homeseer/evc/out"
    Public MQTT_HostAddr As String = "127.0.0.1"
    Private _DevicPollTimer As New System.Timers.Timer
    Private _MessageReaderThread As Thread
    Private _RetryAttempts As Integer = 0
    Private _MqttClient As MqttClient

    Public Sub New()
        '        MyBase.New()
        AddHandler _DevicPollTimer.Elapsed, AddressOf DevicePollTimerTick
    End Sub

    Public Sub client_MqttMessageReceived(sender As Object, e As MqttMsgPublishEventArgs)
        Dim st As String = System.Text.Encoding.Default.GetString(e.Message)
        Logger.LogDebug("message received: {0}", st)
        ProcessResponse(st)
    End Sub

    Public Function Start(Optional ByVal sIP As String = "") As Boolean

        If sIP <> "" Then
            MQTT_HostAddr = sIP ' Allow caller to override configured MQTT address
        End If
        Try
            _MqttClient = New MqttClient(MQTT_HostAddr)
            'register to message received
            AddHandler _MqttClient.MqttMsgPublishReceived, AddressOf client_MqttMessageReceived
            _MqttClient.Connect("EVC Thermostat")
            _MqttClient.Subscribe({MQTT_RecvTopic}, {MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE})

            _MessageReaderThread = New Threading.Thread(AddressOf Comm)
            _MessageReaderThread.Start()
            _RetryAttempts = 0
            Return True
        Catch ex As Exception
            Logger.LogError("Error in Communications Thread Start: {0}", ex.Message)
            If _RetryAttempts < 5 Then
                _RetryAttempts += 1
                Return Start()
            Else
                Logger.LogError("Abandoning Communications Thread Start: {0}", ex.Message)
                Return False
            End If
        End Try
    End Function

    Public Sub Halt()
        _MessageReaderThread.Abort()
    End Sub

    Public Sub Restart()
        Halt()
        Start()
    End Sub

    Public Sub SendCommand(ByVal Command As String)
        Dim tqi As New TransmitQitem
        Try
            tqi.Buf = System.Text.Encoding.Default.GetBytes(Command)
            tqi.Count = Command.Length
            tqi.RetryCount = 0
            TransmitMessageQueue.Enqueue(tqi)
        Catch ex As Exception
            Logger.LogError("Error in SendCMD: {0}", ex.Message)
        End Try
    End Sub

    Private Sub Comm()
        ' init ports here so all work is done in this thread
        Dim st As String = ""
        Dim RetCnt As Integer
        Dim bNewData As Boolean = False


        Do
            Try
                Thread.Sleep(100)

                If TransmitMessageQueue.Count > 0 Then
                    Dim ToWrite As String = ""
                    Do
                        Try
                            Dim tqi As TransmitQitem = TransmitMessageQueue.Dequeue
                            Logger.LogDebug("Writing Data: {0}", System.Text.Encoding.Default.GetString(tqi.Buf))
                            _MqttClient.Publish(MQTT_SendTopic, tqi.Buf)
                            Thread.Sleep(250)
                        Catch ex As Exception
                            Logger.LogError("Error Writing Data: {0}", ex.Message)
                        End Try
                    Loop While TransmitMessageQueue.Count > 0
                End If



            Catch ex As Exception
                Logger.LogError("Error in Poll Thread, {0} Line Number: {1}", ex.Message, Err.Erl)
                st = ""
                Thread.Sleep(10000)
                RetCnt += 1
                If RetCnt = 10 Then
                    Logger.LogError("Poll Thread Error, plugin is shutting down. {0}", ex.Message)
                    _plugin.ShutdownIO()
                End If
            End Try
        Loop

    End Sub

    Private Sub ProcessResponse(ByVal text As String)
        'this area is custom for each thermostat manufacturer
        Dim addr As String
        Dim iStart As Integer
        Dim iLength As Integer
        'Dim Rows() As DataRow
        '       Dim oThermostat As Thermostat = Nothing
        Try
            'extract the qualifier (address) for the thermostat
            iStart = InStr(text, "A=") - 1
            'just keep the data you need
            text = text.Substring(iStart, text.Length - iStart)
            iLength = InStr(1, text, Chr(32)) - 1
            addr = text.Substring(0, iLength)
            addr = Strings.Right(addr, addr.Length - 2)
            addr = addr.Trim
            Logger.LogDebug("Processing Dataline - {0}", text)
            _plugin.ProcessDataReceived(addr, text)
        Catch ex As Exception
            Logger.LogError("Error in ProcessResponse, {0} Line Number: {1}", ex.Message, Err.Erl)
            Logger.LogError("Error in ProcessResponse, return dataline: {0}", text)
        End Try
    End Sub

    Public Sub SetPolling(Optional ByVal Interval As Integer = -1)
        'the polling timer is seperate from the comm thread loop
        DevicePollTimerInterval = Interval
        _DevicPollTimer.Enabled = False
        If DevicePollTimerInterval > 0 Then
            _DevicPollTimer.Interval = DevicePollTimerInterval * 1000 'interval is in milliseconds, so adjust the interval number
            _DevicPollTimer.Enabled = True
        End If
    End Sub

    Private Sub DevicePollTimerTick()
        _plugin.Poll()
    End Sub

    Public Sub SetSendTopic(Optional ByVal Topic As String = "homeseer/evc/out")
        MQTT_SendTopic = Topic
    End Sub

    Public Sub SetRecvTopic(Optional ByVal Topic As String = "homeseer/evc/in")
        MQTT_RecvTopic = Topic
    End Sub

    Public Sub SetMQTTHost(Optional ByVal Addr As String = "127.0.0.1")
        MQTT_HostAddr = Addr
    End Sub

    Private Class TransmitQitem
        Public Buf() As Byte
        Public Count As Integer
        Public RetryCount As Integer

        Public Sub New()
            RetryCount = 0
        End Sub
    End Class
End Class
