Imports System.Text
Imports HomeSeer.Jui.Types
Imports HomeSeer.Jui.Views
Imports HomeSeer.PluginSdk
Imports HomeSeer.PluginSdk.Devices
Imports HomeSeer.PluginSdk.Devices.Identification
Imports HomeSeer.PluginSdk.Logging
Imports Newtonsoft.Json

''' <inheritdoc cref="AbstractPlugin"/>
''' <summary>
''' The plugin class for HomeSeer Sample Plugin that implements the <see cref="AbstractPlugin"/> base class.
''' </summary>
''' <remarks>
''' This class is accessed by HomeSeer and requires that its name be "HSPI" and be located in a namespace
'''  that corresponds to the name of the executable. For this plugin, "HomeSeerThermostatPluginVB" the executable
'''  file is "HSPI_HomeSeerThermostatPluginVB.exe" and this class is HSPI_HomeSeerThermostatPluginVB.HSPI
''' <para>
''' If HomeSeer is unable to find this class, the plugin will not start.
''' </para>
''' </remarks>
Public Class HSPI
    Inherits AbstractPlugin

    'speaker client instance
    Private _speakerClient As SpeakerClient
    Private _commThread As MessagingThread
    Private _addressRefDict As Dictionary(Of Integer, Integer)


    ''' <inheritdoc />
    ''' <remarks>
    ''' This ID is used to identify the plugin and should be unique across all plugins
    ''' <para>
    ''' This must match the MSBuild property $(PluginId) as this will be used to copy
    '''  all of the HTML feature pages located in .\html\ to a relative directory
    '''  within the HomeSeer html folder.
    ''' </para>
    ''' <para>
    ''' The relative address for all of the HTML pages will end up looking like this:
    '''  ..\Homeseer\Homeseer\html\HomeSeerThermostatPluginVB\
    ''' </para>
    ''' </remarks>
    Public Overrides ReadOnly Property Id As String
        Get
            Return "HomeSeerThermostatPluginVB"
        End Get
    End Property

    ''' <inheritdoc />
    ''' <remarks>
    ''' This is the readable name for the plugin that is displayed throughout HomeSeer
    ''' </remarks>
    Public Overrides ReadOnly Property Name As String
        Get
            Return "Thermostat Plugin VB"
        End Get
    End Property

    ''' <inheritdoc />
    Protected Overrides ReadOnly Property SettingsFileName As String
        Get
            Return "HomeSeerThermostatPluginVB.ini"
        End Get
    End Property

    Public Overrides ReadOnly Property SupportsConfigDevice As Boolean
        Get
            Return True
        End Get
    End Property

    Public Sub New()
        'Initialize the plugin 

        'Enable internal debug logging to console
        LogDebug = True
        'Setup anything that needs to be configured before a connection to HomeSeer is established
        ' like initializing the starting state of anything needed for the operation of the plugin

        'Such as initializing the settings pages presented to the user (currently saved state is loaded later)
        InitializeSettingsPages()

        'Or adding an event action or trigger type definition to the list of types supported by your plugin
        'ActionTypes.AddActionType(GetType(WriteLogSampleActionType))
        TriggerTypes.AddTriggerType(GetType(SampleTriggerType))
    End Sub

    ''' <summary>
    ''' Initialize the starting state of the settings pages for the HomeSeerThermostatPlugin.
    '''  This constructs the framework that the user configurable settings for the plugin live in.
    '''  Any saved configuration options are loaded later in <see cref="Initialize"/> using
    '''  <see cref="AbstractPlugin.LoadSettingsFromIni"/>
    ''' </summary>
    ''' <remarks>
    ''' For ease of use throughout the plugin, all of the view IDs, names, and values (non-volatile data)
    '''  are stored in the <see cref="HSPI_HomeSeerThermostatPluginVB.Constants.Settings"/> static class.
    ''' </remarks>
    Private Sub InitializeSettingsPages()
        'Initialize the settings page

        'Start a PageFactory to construct the Page
        Dim settingsPage = PageFactory.CreateSettingsPage(Constants.Settings.SettingsPageId, Constants.Settings.SettingsPageName)

        'Add a text InputView to the page
        settingsPage.WithInput(Constants.Settings.SpHostAddressId, Constants.Settings.SpHostAddressName, Constants.Settings.SpHostAddressDefault, EInputType.Text)
        'Add a text InputView to the page
        settingsPage.WithInput(Constants.Settings.SpReceiveTopicId, Constants.Settings.SpReceiveTopicName, Constants.Settings.SpReceiveTopicDefault, EInputType.Text)
        'Add a text InputView to the page
        settingsPage.WithInput(Constants.Settings.SpSendTopicId, Constants.Settings.SpSendTopicName, Constants.Settings.SpSendTopicDefault, EInputType.Text)
        'Add a number InputView to the page
        settingsPage.WithInput(Constants.Settings.SpPollIntervalId, Constants.Settings.SpPollIntervalName, Constants.Settings.SpPollIntervalDefault, EInputType.Number)
        'Create 2 ToggleViews for controlling the visibility of the other two settings pages

        Dim pageToggles = New List(Of ToggleView) From {
            New ToggleView(Constants.Settings.SpDebugEnableToggleId, Constants.Settings.SpDebugEnableToggleName, False)
        }
        'Add a ViewGroup containing all of the ToggleViews to the page
        settingsPage.WithGroup(Constants.Settings.SpDebugToggleGroupId, Constants.Settings.SpDebugToggleGroupName, pageToggles)

        Settings.Add(settingsPage.Page)

    End Sub

    Protected Overrides Sub Initialize()
        'Load the state of Settings saved to INI if there are any.
        LoadSettingsFromIni()
        If LogDebug Then
            Console.WriteLine("Registering feature pages")
        End If
        'Initialize feature pages
        'HomeSeerSystem.RegisterFeaturePage(Id, "sample-guided-process.html", "Sample Guided Process")
        'HomeSeerSystem.RegisterFeaturePage(Id, "sample-blank.html", "Sample Blank Page")
        'HomeSeerSystem.RegisterFeaturePage(Id, "sample-trigger-feature.html", "Trigger Feature Page")
        'HomeSeerSystem.RegisterFeaturePage(Id, "sample-functions.html", "Plugin Functions Sample")
        'HomeSeerSystem.RegisterDeviceIncPage(Id, "add-sample-device.html", "Add Sample Device")
        EnableDebugLog = Settings.Item(Constants.Settings.SettingsPageId).GetViewById(Constants.Settings.SpDebugEnableToggleId).GetStringValue()

        ' If a speaker client Is needed that handles sending speech to an audio device, initialize that here.
        ' If you are supporting multiple speak devices such as multiple speakers, you would make this call
        ' in your reoutine that initializes each speaker device. Create a New instance of the speaker client
        ' for each speaker. We simply initalize one here as a sample implementation
        _speakerClient = New SpeakerClient(Name)
        ' if the HS system has the setting "No password required for local subnet" enabled, the user/pass passed to Connect are ignored
        ' if the connection Is from the local subnet, else the user/pass passed here are must exist as a user in the system
        ' You will need to allow the user to supply a user/pass in your plugin settings
        ' This functions connects your speaker client to the system. Your client will then appear as a speaker client in the system
        ' And can be selected as a target for speech And audio in event actions.
        ' When the system speaks to your client, your SpeakText function Is called in SpeakerClient class
        _speakerClient.Connect("default", "default", HomeSeerSystem.GetIpAddress)

        _commThread = New MessagingThread()

        _commThread.MQTT_HostAddr = Settings.Item(Constants.Settings.SettingsPageId).GetViewById(Constants.Settings.SpHostAddressId).GetStringValue()
        _commThread.MQTT_RecvTopic = Settings.Item(Constants.Settings.SettingsPageId).GetViewById(Constants.Settings.SpReceiveTopicId).GetStringValue()
        _commThread.MQTT_SendTopic = Settings.Item(Constants.Settings.SettingsPageId).GetViewById(Constants.Settings.SpSendTopicId).GetStringValue()
        _commThread.DevicePollTimerInterval = Settings.Item(Constants.Settings.SettingsPageId).GetViewById(Constants.Settings.SpPollIntervalId).GetStringValue()

        _commThread.Start()

        UpdateDeviceReferenceCache()

        Add_HSThermostatDevice("Bedroom", 1)
        Add_HSThermostatDevice("Family Room", 2)
        Add_HSThermostatDevice("Kitchen", 3)
        Add_HSThermostatDevice("Parlor", 4)
        Add_HSThermostatDevice("Foyer", 5)
        Add_HSThermostatDevice("Studio", 6)
        Add_HSThermostatDevice("Turtle Room", 7)
        Add_HSThermostatDevice("File Room", 8)
        Add_HSThermostatDevice("Guest Room", 9)
        Add_HSThermostatDevice("Loft", 10)

        Console.WriteLine("Initialized")
        Status = PluginStatus.Ok()
    End Sub

    Protected Overrides Sub OnShutdown()
        Logger.LogDebug("Thermostat Plugin Shutting down")
        _speakerClient.Disconnect()
    End Sub


    Protected Overrides Function OnSettingChange(pageId As String, currentView As AbstractView, changedView As AbstractView) As Boolean

        If pageId = Constants.Settings.SettingsPageId Then
             If changedView.Id = Constants.Settings.SpHostAddressId Then
                _commThread.MQTT_HostAddr = changedView.GetStringValue()
                _commThread.Restart()
            ElseIf changedView.Id = Constants.Settings.SpReceiveTopicId Then
                _commThread.MQTT_RecvTopic = changedView.GetStringValue()
                _commThread.Restart()
            ElseIf changedView.Id = Constants.Settings.SpSendTopicId Then
                _commThread.MQTT_SendTopic = changedView.GetStringValue()
                _commThread.Restart()
            ElseIf changedView.Id = Constants.Settings.SpPollIntervalId Then
                _commThread.DevicePollTimerInterval = changedView.GetStringValue()
                _commThread.Restart()
            ElseIf changedView.Id = Constants.Settings.SpDebugEnableToggleId Then
                EnableDebugLog = changedView.GetStringValue()
            Else
                If LogDebug Then
                    Console.WriteLine($"View ID {changedView.Id} does not match any views on the page.")
                End If
            End If
        End If

        Return True
    End Function

    ''' <inheritdoc />
    ''' <remarks>
    ''' This plugin does not have a shifting operational state; so this method is not used.
    ''' </remarks>
    Protected Overrides Sub BeforeReturnStatus()
    End Sub

    Public Overrides Function GetJuiDeviceConfigPage(ByVal deviceRef As Integer) As String
        Dim Schedule As Dictionary(Of String, Dictionary(Of String, Period)) = New Dictionary(Of String, Dictionary(Of String, Period))
        Dim Day As Dictionary(Of String, Period) = New Dictionary(Of String, Period)
        Dim Per As Period = New Period

        Dim scheduleValue As String = GetExtraData(deviceRef, DeviceConfigScheduleId)
        Dim toggleValue As Boolean = GetExtraData(deviceRef, DeviceConfigSampleToggleId) = True.ToString()
        Dim checkboxValue As Boolean = GetExtraData(deviceRef, DeviceConfigSampleCheckBoxId) = True.ToString()
        Dim dropdownSavedValue As String = GetExtraData(deviceRef, DeviceConfigSelectListId)
        Dim dropdownValue As Integer = -1

        If Not String.IsNullOrEmpty(dropdownSavedValue) Then
            dropdownValue = Convert.ToInt32(dropdownSavedValue)
        End If

        Dim radioSelectSavedValue As String = GetExtraData(deviceRef, DeviceConfigRadioSlId)
        Dim radioSelectValue As Integer = -1

        If Not String.IsNullOrEmpty(radioSelectSavedValue) Then
            radioSelectValue = Convert.ToInt32(radioSelectSavedValue)
        End If

        Dim inputSavedValue As String = GetExtraData(deviceRef, DeviceConfigInputId)
        Dim inputValue As String = DeviceConfigInputValue

        If Not String.IsNullOrEmpty(inputSavedValue) Then
            inputValue = inputSavedValue
        End If


        Dim textAreaSavedValue As String = GetExtraData(deviceRef, DeviceConfigScheduleId)
        Dim textAreaValue As String = ""

        Schedule = JsonConvert.DeserializeObject(Of Dictionary(Of String, Dictionary(Of String, Period)))(textAreaSavedValue)

        For Each i In Schedule
            For Each d In i.Value
                Dim p As Period = d.Value
                Console.WriteLine("Day {0} Period {1} Time Of Day {2}", i.Key, d.Key, p.Time.TimeOfDay)
                Console.WriteLine("Day {0} Period {1} Cool Set Point {2}", i.Key, d.Key, p.Cool)
                Console.WriteLine("Day {0} Period {1} Heat Set Point {2}", i.Key, d.Key, p.Heat)
                Console.WriteLine("Day {0} Period {1} Fan Setting {2}", i.Key, d.Key, p.Fan)
            Next
        Next
        UpdatePhysicalDeviceSchedule(HomeSeerSystem.GetDeviceByRef(deviceRef).Address, Schedule)

        If Not String.IsNullOrEmpty(textAreaSavedValue) Then
            textAreaValue = textAreaSavedValue
        End If
        Dim timeSpanSavedValue As String = GetExtraData(deviceRef, DeviceConfigTimeSpanId)
        Dim timeSpanValue As TimeSpan = TimeSpan.Zero

        If Not String.IsNullOrEmpty(timeSpanSavedValue) Then
            TimeSpan.TryParse(timeSpanSavedValue, timeSpanValue)
        End If
        Dim deviceConfigPage = PageFactory.CreateDeviceConfigPage(DeviceConfigPageId, DeviceConfigPageName)
        deviceConfigPage.WithLabel(DeviceConfigLabelWTitleId, DeviceConfigLabelWTitleName, DeviceConfigLabelWTitleValue)
        deviceConfigPage.WithLabel(DeviceConfigLabelWoTitleId, Nothing, DeviceConfigLabelWoTitleValue)
        deviceConfigPage.WithToggle(DeviceConfigSampleToggleId, DeviceConfigSampleToggleName, toggleValue)
        deviceConfigPage.WithCheckBox(DeviceConfigSampleCheckBoxId, DeviceConfigSampleCheckBoxName, checkboxValue)
        deviceConfigPage.WithDropDownSelectList(DeviceConfigSelectListId, DeviceConfigSelectListName, DeviceConfigSelectListOptions, dropdownValue)
        deviceConfigPage.WithRadioSelectList(DeviceConfigRadioSlId, DeviceConfigRadioSlName, DeviceConfigSelectListOptions, radioSelectValue)
        deviceConfigPage.WithInput(DeviceConfigInputId, DeviceConfigInputName, inputValue)
        deviceConfigPage.WithTextArea(DeviceConfigTextAreaId, DeviceConfigTextAreaName, textAreaValue)
        deviceConfigPage.WithTimeSpan(DeviceConfigTimeSpanId, DeviceConfigTimeSpanName, timeSpanValue, True, False)
        Return deviceConfigPage.Page.ToJsonString()
    End Function

    Protected Overrides Function OnDeviceConfigChange(ByVal deviceConfigPage As Page, ByVal deviceRef As Integer) As Boolean
        For Each view As AbstractView In deviceConfigPage.Views

            If view.Id = DeviceConfigSampleToggleId Then
                Dim v As ToggleView = TryCast(view, ToggleView)

                If v IsNot Nothing Then
                    SetExtraData(deviceRef, DeviceConfigSampleToggleId, v.IsEnabled.ToString())
                End If
            ElseIf view.Id = DeviceConfigSampleCheckBoxId Then
                Dim v As ToggleView = TryCast(view, ToggleView)

                If v IsNot Nothing Then
                    SetExtraData(deviceRef, DeviceConfigSampleCheckBoxId, v.IsEnabled.ToString())
                End If
            ElseIf view.Id = DeviceConfigSelectListId Then
                Dim v As SelectListView = TryCast(view, SelectListView)

                If v IsNot Nothing Then
                    SetExtraData(deviceRef, DeviceConfigSelectListId, v.Selection.ToString())
                End If
            ElseIf view.Id = DeviceConfigRadioSlId Then
                Dim v As SelectListView = TryCast(view, SelectListView)

                If v IsNot Nothing Then
                    SetExtraData(deviceRef, DeviceConfigRadioSlId, v.Selection.ToString())
                End If
            ElseIf view.Id = DeviceConfigInputId Then
                Dim v As InputView = TryCast(view, InputView)

                If v IsNot Nothing Then
                    SetExtraData(deviceRef, DeviceConfigInputId, v.Value)
                End If
            ElseIf view.Id = DeviceConfigTextAreaId Then
                Dim v As TextAreaView = TryCast(view, TextAreaView)

                If v IsNot Nothing Then
                    SetExtraData(deviceRef, DeviceConfigTextAreaId, v.Value)
                End If
            ElseIf view.Id = DeviceConfigTimeSpanId Then
                Dim v As TimeSpanView = TryCast(view, TimeSpanView)

                If v IsNot Nothing Then
                    SetExtraData(deviceRef, DeviceConfigTimeSpanId, v.GetStringValue())
                End If
            End If
        Next

        Return True
    End Function

    ''' <inheritdoc />
    ''' <remarks>
    ''' Process any HTTP POST requests targeting pages registered to your plugin.
    ''' <para>
    ''' This is a very flexible process that does not have a predefined structure. The form <see cref="data"/> sends
    '''  from a page is entirely up to you and what works for you.  JSON and Base64 strings are encouraged because
    '''  of how readily available resources are to translate to/from these types. In Javascript, see JSON.stringify();
    '''  and window.btoa();
    ''' </para>
    ''' </remarks>
    Public Overrides Function PostBackProc(page As String, data As String, user As String, userRights As Integer) As String
        If LogDebug Then
            Console.WriteLine("PostBack")
        End If

        Dim response = ""

        Select Case page
            Case "sample-trigger-feature.html"

                'Handle the Trigger Feature page
                Try
                    Dim triggerOptions = JsonConvert.DeserializeObject(Of List(Of Boolean))(data)

                    'Get all triggers configured on the HomeSeer system that are of the SampleTriggerType
                    Dim configuredTriggers = HomeSeerSystem.GetTriggersByType(Id, SampleTriggerType.TriggerNumber)

                    If configuredTriggers.Length = 0 Then
                        Return "No triggers configured to fire."
                    End If

                    'Handle each trigger that matches
                    For Each configuredTrigger In configuredTriggers
                        Dim trig = New SampleTriggerType(configuredTrigger, Me, LogDebug)

                        If trig.ShouldTriggerFire(triggerOptions.ToArray()) Then
                            HomeSeerSystem.TriggerFire(Id, configuredTrigger)
                        End If
                    Next

                Catch exception As JsonSerializationException
                    If LogDebug Then
                        Console.WriteLine(exception)
                    End If
                    response = $"Error while deserializing data: {exception.Message}"
                End Try

            Case "add-sample-device.html"
                Try
                    Dim postData = JsonConvert.DeserializeObject(Of DeviceAddPostData)(data)
                    If LogDebug Then
                        Console.WriteLine("Post back from add-sample-device page")
                    End If
                    If postData.Action = "verify" Then
                        response = JsonConvert.SerializeObject(postData.Device)
                    Else
                        Dim deviceData = postData.Device
                        Dim device = deviceData.BuildDevice(Id)
                        Dim devRef = HomeSeerSystem.CreateDevice(device)
                        deviceData.Ref = devRef
                        response = JsonConvert.SerializeObject(deviceData)
                    End If

                Catch exception As Exception
                    If LogDebug Then
                        Console.WriteLine(exception.Message)
                    End If
                    response = "error"
                End Try

            Case Else
                response = "error"
        End Select

        Return response
    End Function

    ''' <summary>
    ''' Called by the sample trigger feature page to get the HTML for a list of checkboxes to use a trigger options
    ''' <para>
    ''' {{list=plugin_function 'HomeSeerThermostatPluginVB' 'GetTriggerOptionsHtml' [2]}}
    ''' </para>
    ''' </summary>
    ''' <param name="numTriggerOptions">The number of checkboxes to generate</param>
    ''' <returns>
    ''' A List of HTML strings representing checkbox input elements
    ''' </returns>
    Public Function GetTriggerOptionsHtml(ByVal numTriggerOptions As Integer) As List(Of String)
        Dim triggerOptions = New List(Of String)()

        For i = 1 To numTriggerOptions
            Dim cbTrigOpt = New ToggleView($"liquid-checkbox-triggeroption{i}", $"Trigger Option {i}") With {
                .ToggleType = EToggleType.Checkbox
            }
            triggerOptions.Add(cbTrigOpt.ToHtml())
        Next

        Return triggerOptions
    End Function

    ''' <summary>
    ''' Called by the sample trigger feature page to get trigger option items as a list to populate HTML on the page.
    ''' <para>
    ''' {{list2=plugin_function 'HomeSeerThermostatPluginVB' 'GetTriggerOptions' [2]}}
    ''' </para>
    ''' </summary>
    ''' <param name="numTriggerOptions">The number of trigger options to generate.</param>
    ''' <returns>
    ''' A List of <see cref="TriggerOptionItem"/>s used for checkbox input HTML element IDs and Names
    ''' </returns>
    Public Function GetTriggerOption(ByVal numTriggerOptions As Integer) As List(Of TriggerOptionItem)
        Dim triggerOptions = New List(Of TriggerOptionItem)()

        For i = 1 To numTriggerOptions
            triggerOptions.Add(New TriggerOptionItem(i, $"Trigger Option {i}"))
        Next

        Return triggerOptions
    End Function

    '<inheritdoc />
    Public Sub WriteLog(ByVal logType As ELogType, ByVal message As String)
        HomeSeerSystem.WriteLog(logType, message, Name)
    End Sub

    Class Period

        Private _time As DateTime
        Private _heat As Integer
        Private _cool As Integer
        Private _fan As Integer
        <JsonIgnore>
        Private _hasChanged As Boolean = False

        Property Time As DateTime
            Get
                Return _time
            End Get
            Set(value As DateTime)
                _time = value
                _hasChanged = True
            End Set
        End Property
        Property Heat As Integer
            Get
                Return _heat
            End Get
            Set(value As Integer)
                If value > 40 And value < 99 Then
                    _heat = value
                    _hasChanged = True
                Else
                    Throw New ArgumentException("Value out of range <40-99>")
                End If

            End Set
        End Property
        Property Cool As Integer
            Get
                Return _cool
            End Get
            Set(value As Integer)
                If value > 40 And value < 99 Then
                    _cool = value
                    _hasChanged = True
                Else
                    Throw New ArgumentException("Value out of range <40-99>")
                End If

            End Set
        End Property
        Property Fan As Integer
            Get
                Return _fan
            End Get
            Set(value As Integer)
                If value <> Controls.EControlUse.ThermFanAuto And value <> Controls.EControlUse.ThermFanOn Then
                    _fan = value
                    _hasChanged = True
                Else
                    Throw New ArgumentException("Value Invalid, must be On ThermFanOn or ThermFanOff")
                End If

            End Set
        End Property

        ReadOnly Property HasChanged As Boolean
            Get
                Return _hasChanged
            End Get
        End Property

        Public Sub Clean()
            _hasChanged = False
        End Sub
    End Class

    Public Property EnableDebugLog() As Boolean
        Get
            Return LogDebug
        End Get
        Set(ByVal value As Boolean)
            Logger.LogInfo("Debug flag changed to {0}", value)
            LogDebug = value
        End Set
    End Property

    Private Function GetExtraData(ByVal deviceRef As Integer, ByVal key As String) As String
        Dim extraData As PlugExtraData = CType(HomeSeerSystem.GetPropertyByRef(deviceRef, EProperty.PlugExtraData), PlugExtraData)

        If extraData IsNot Nothing AndAlso extraData.ContainsNamed(key) Then
            Return extraData(key)
        End If

        Return ""
    End Function

    Private Sub SetExtraData(ByVal deviceRef As Integer, ByVal key As String, ByVal value As String)
        Dim extraData As PlugExtraData = CType(HomeSeerSystem.GetPropertyByRef(deviceRef, EProperty.PlugExtraData), PlugExtraData)

        If extraData Is Nothing Then
            extraData = New PlugExtraData()
        End If

        extraData(key) = value
        HomeSeerSystem.UpdatePropertyByRef(deviceRef, EProperty.PlugExtraData, extraData)
    End Sub


#Region "Thermostat Routines"
    Public Overrides Sub SetIOMulti(colSend As List(Of Controls.ControlEvent))
        Dim CC As Controls.ControlEvent
        Dim ParentRef As String
        Dim fv As HsFeature
        Dim dv As HsDevice

        For Each CC In colSend
            'we need the root device ref of the group, so find out if we already have it.
            fv = HomeSeerSystem.GetFeatureByRef(CC.TargetRef)
            If fv.Relationship = ERelationship.Feature Then
                'it's a feature device so the root of the group is the first associated device
                ParentRef = fv.AssociatedDevices(0)
            Else
                'it's the root so use that ref
                ParentRef = fv.Ref
            End If
            dv = HomeSeerSystem.GetDeviceByRef(ParentRef)
            HomeSeerSystem.UpdateFeatureValueByRef(CC.TargetRef, CC.ControlValue)
            UpdatePhysicalDevice(dv.Address, fv.TypeInfo.SubType, CC.ControlValue)
            'See if we need to fire any of our triggers on the events page
            'CheckTriggers(ParentRef)
        Next
    End Sub

    Sub Add_HSThermostatDevice(ByVal DeviceName As String, ByVal Address As Integer)
        Dim dd As HomeSeer.PluginSdk.Devices.NewDeviceData
        Dim df As HomeSeer.PluginSdk.Devices.DeviceFactory
        Dim tr As ValueRange

        If _addressRefDict.ContainsKey(Address) Then
            Logger.LogError("Device with Address {0} already exists DeviceRef {1}", Address, _addressRefDict.Item(Address))
            Return
        End If

        'Use the device factory to create an area to hold the device data that is used to create the device
        df = HomeSeer.PluginSdk.Devices.DeviceFactory.CreateDevice(Id)
        'set the name of the device.
        df.WithName(DeviceName)
        'set the type of the device.
        df.AsType(EDeviceType.Thermostat, 0)
        df.WithAddress(Address)

        'this is the what you use to create feature(child) devices for your device group
        Dim ff As HomeSeer.PluginSdk.Devices.FeatureFactory

        ' status features

        'create a new feature data holder.
        ff = HomeSeer.PluginSdk.Devices.FeatureFactory.CreateFeature(Id)

        'add the properties for your feature.
        ff.WithName("Temperature")
        ff.WithDisplayType(EFeatureDisplayType.Normal)
        ff.WithMiscFlags({EMiscFlag.StatusOnly, EMiscFlag.ShowValues})
        ff.AsType(EFeatureType.ThermostatStatus, EThermostatStatusFeatureSubType.Temperature)
        ' need to add a value range and suffix here
        'tr = New ValueRange(0.0, 120.0)
        'tr.Suffix = "°"
        'Dim sg = New StatusGraphic("images/evcstat/thermostat-sub.png", tr)

        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-00.png", -32.0, 0.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-10.png", 0.000001, 10.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-20.png", 10.000001, 20.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-30.png", 20.000001, 30.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-40.png", 30.000001, 40.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-50.png", 40.000001, 50.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-60.png", 50.000001, 60.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-70.png", 60.000001, 70.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-80.png", 70.000001, 80.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-90.png", 80.000001, 90.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-100.png", 90.000001, 100.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-110.png", 100.000001, 120.0)

        'we're gonna toggle the value to update the datechanged on the device
        'need to add to feature here
        'We need the value to be unique to the statuses to allow the button to be activated regardless of the status of the device.


        'Add the feature data to the device data in the device factory
        df.WithFeature(ff)

        'create a new feature data holder.
        ff = HomeSeer.PluginSdk.Devices.FeatureFactory.CreateFeature(Id)

        'add the properties for your feature.
        ff.WithName("Calling")
        ff.WithDisplayType(EFeatureDisplayType.Normal)
        ff.WithMiscFlags({EMiscFlag.StatusOnly, EMiscFlag.ShowValues})
        ff.AsType(EFeatureType.ThermostatStatus, EThermostatStatusFeatureSubType.OperatingState)

        ff.AddGraphicForValue("images/HomeSeer/status/fan-state-off.png", False, "Idle")
        ff.AddGraphicForValue("images/HomeSeer/status/fan-state-on.png", True, "Running")
        'we're gonna toggle the value to update the datechanged on the device

        'We need the value to be unique to the statuses to allow the button to be activated regardless of the status of the device.


        'Add the feature data to the device data in the device factory
        df.WithFeature(ff)

        'create a new feature data holder.
        ff = HomeSeer.PluginSdk.Devices.FeatureFactory.CreateFeature(Id)

        'add the properties for your feature.
        ff.WithName("Outdoor Temperature")
        ff.WithDisplayType(EFeatureDisplayType.Normal)
        ff.WithMiscFlags({EMiscFlag.StatusOnly, EMiscFlag.ShowValues})
        ff.AsType(EFeatureType.ThermostatStatus, EThermostatStatusFeatureSubType.TemperatureOther)

        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-00.png", -32.0, 0.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-10.png", 0.000001, 10.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-20.png", 10.000001, 20.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-30.png", 20.000001, 30.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-40.png", 30.000001, 40.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-50.png", 40.000001, 50.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-60.png", 50.000001, 60.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-70.png", 60.000001, 70.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-80.png", 70.000001, 80.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-90.png", 80.000001, 90.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-100.png", 90.000001, 100.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-110.png", 100.000001, 120.0)

        'we're gonna toggle the value to update the datechanged on the device

        'We need the value to be unique to the statuses to allow the button to be activated regardless of the status of the device.


        'Add the feature data to the device data in the device factory
        df.WithFeature(ff)


        'control features 

        'create a new feature data holder.
        ff = HomeSeer.PluginSdk.Devices.FeatureFactory.CreateFeature(Id)

        'add the properties for your feature.
        ff.WithName("Cool Set Point")
        ff.WithDisplayType(EFeatureDisplayType.Normal)
        ff.AsType(EFeatureType.ThermostatControl, EThermostatControlFeatureSubType.CoolingSetPoint)

        'we're gonna toggle the value to update the datechanged on the device
        '        ff.AddGraphicForValue("images/evcstat/thermostat-sub.png", 0, "Set Point")
        'We need the value to be unique to the statuses to allow the button to be activated regardless of the status of the device.
        tr = New ValueRange(40.0, 99.0)

        ff.AddSlider(tr, Nothing, Controls.EControlUse.CoolSetPoint)

        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-00.png", -32.0, 0.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-10.png", 0.000001, 10.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-20.png", 10.000001, 20.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-30.png", 20.000001, 30.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-40.png", 30.000001, 40.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-50.png", 40.000001, 50.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-60.png", 50.000001, 60.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-70.png", 60.000001, 70.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-80.png", 70.000001, 80.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-90.png", 80.000001, 90.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-100.png", 90.000001, 100.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-110.png", 100.000001, 120.0)


        'Add the feature data to the device data in the device factory
        df.WithFeature(ff)

        'create a new feature data holder.
        ff = HomeSeer.PluginSdk.Devices.FeatureFactory.CreateFeature(Id)

        'add the properties for your feature.
        ff.WithName("Heat Set Point")
        ff.WithDisplayType(EFeatureDisplayType.Normal)
        ff.AsType(EFeatureType.ThermostatControl, EThermostatControlFeatureSubType.HeatingSetPoint)

        'we're gonna toggle the value to update the datechanged on the device
        '       ff.AddGraphicForValue("images/evcstat/thermostat-sub.png", 0, "Set Point")
        'We need the value to be unique to the statuses to allow the button to be activated regardless of the status of the device.

        ff.AddSlider(tr, Nothing, Controls.EControlUse.HeatSetPoint)

        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-00.png", -32.0, 0.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-10.png", 0.000001, 10.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-20.png", 10.000001, 20.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-30.png", 20.000001, 30.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-40.png", 30.000001, 40.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-50.png", 40.000001, 50.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-60.png", 50.000001, 60.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-70.png", 60.000001, 70.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-80.png", 70.000001, 80.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-90.png", 80.000001, 90.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-100.png", 90.000001, 100.0)
        ff.AddGraphicForRange("images/HomeSeer/status/Thermometer-110.png", 100.000001, 120.0)

        'Add the feature data to the device data in the device factory
        df.WithFeature(ff)


        ff = HomeSeer.PluginSdk.Devices.FeatureFactory.CreateFeature(Id)

        'add the properties for your feature.
        ff.WithName("Fan Mode")
        ff.WithDisplayType(EFeatureDisplayType.Normal)
        ff.AsType(EFeatureType.ThermostatControl, EThermostatControlFeatureSubType.FanModeSet)

        'we're gonna toggle the value to update the datechanged on the device
        '      ff.AddGraphicForValue("images/evcstat/thermostat-sub.png", 0, "Fan Mode")
        'We need the value to be unique to the statuses to allow the button to be activated regardless of the status of the device.

        ff.AddButton(Controls.EControlUse.ThermFanAuto, "Auto", Nothing, Controls.EControlUse.ThermFanAuto)
        ff.AddButton(Controls.EControlUse.ThermFanOn, "On", Nothing, Controls.EControlUse.ThermFanOn)
        ff.AddGraphicForValue("images/HomeSeer/status/fan-auto.png", Controls.EControlUse.ThermFanAuto, "Auto")
        ff.AddGraphicForValue("images/HomeSeer/status/fan-on.png", Controls.EControlUse.ThermFanOn, "On")


        'Add the feature data to the device data in the device factory
        df.WithFeature(ff)

        ff = HomeSeer.PluginSdk.Devices.FeatureFactory.CreateFeature(Id)

        'add the properties for your feature.
        ff.WithName("Hold Mode")
        ff.WithDisplayType(EFeatureDisplayType.Normal)
        ff.AsType(EFeatureType.ThermostatControl, EThermostatControlFeatureSubType.HoldMode)

        'we're gonna toggle the value to update the datechanged on the device
        '        ff.AddGraphicForValue("images/evcstat/thermostat-sub.png", 0, "Hold Status")
        'We need the value to be unique to the statuses to allow the button to be activated regardless of the status of the device.

        ff.AddButton(Controls.EControlUse.Off, "Off", Nothing, Controls.EControlUse.Off)
        ff.AddButton(Controls.EControlUse.On, "On", Nothing, Controls.EControlUse.On)
        ff.AddButton(Controls.EControlUse.OnAlternate, "Tmp", Nothing, Controls.EControlUse.OnAlternate)
        ff.AddGraphicForValue("images/HomeSeer/status/off.gif", Controls.EControlUse.Off, "Off")
        ff.AddGraphicForValue("images/HomeSeer/status/on.gif", Controls.EControlUse.On, "On")
        ff.AddGraphicForValue("images/HomeSeer/status/pause.png", Controls.EControlUse.OnAlternate, "Tmp")

        'Add the feature data to the device data in the device factory
        df.WithFeature(ff)

        ff = HomeSeer.PluginSdk.Devices.FeatureFactory.CreateFeature(Id)

        'add the properties for your feature.
        ff.WithName("Operating Mode")
        ff.WithDisplayType(EFeatureDisplayType.Normal)
        ff.AsType(EFeatureType.ThermostatControl, EThermostatControlFeatureSubType.ModeSet)

        'we're gonna toggle the value to update the datechanged on the device
        '       ff.AddGraphicForValue("images/evcstat/thermostat-sub.png", 0, "Mode")
        'We need the value to be unique to the statuses to allow the button to be activated regardless of the status of the device.

        ff.AddButton(Controls.EControlUse.ThermModeAuto, "Auto", Nothing, Controls.EControlUse.ThermModeAuto)
        ff.AddButton(Controls.EControlUse.ThermModeCool, "Cool", Nothing, Controls.EControlUse.ThermModeCool)
        ff.AddButton(Controls.EControlUse.ThermModeHeat, "Heat", Nothing, Controls.EControlUse.ThermModeHeat)
        ff.AddButton(Controls.EControlUse.ThermModeOff, "Off", Nothing, Controls.EControlUse.ThermModeOff)
        ff.AddGraphicForValue("images/HomeSeer/status/auto-mode.png", Controls.EControlUse.ThermModeAuto, "Auto")
        ff.AddGraphicForValue("images/HomeSeer/status/Cool.png", Controls.EControlUse.ThermModeCool, "Cool")
        ff.AddGraphicForValue("images/HomeSeer/status/Heat.png", Controls.EControlUse.ThermModeHeat, "Heat")
        ff.AddGraphicForValue("images/HomeSeer/status/modeoff.png", Controls.EControlUse.ThermModeOff, "Off")

        'Add the feature data to the device data in the device factory
        df.WithFeature(ff)

        'Put device specific data with the device. (this is the cameradata class)
        Dim PED As New HomeSeer.PluginSdk.Devices.PlugExtraData
        '        PED.AddNamed(DeviceConfigScheduleId, Newtonsoft.Json.Linq.JObject.Parse("{""period"":[{""time"":570,""heat"":74,""cool"":78,""fan"":4},{""time"":2047,""heat"":62,""cool"":85,""fan"":4},{""time"":2047,""heat"":70,""cool"":78,""fan"":4},{""time"":1380,""heat"":74,""cool"":78,""fan"":4}]}").ToString())
        PED.AddNamed(DeviceConfigScheduleId, "")
        df.WithExtraData(PED)

        'this bundles all the needed data from the device to send to HomeSeer.
        dd = df.PrepareForHs

        'this creates the device in HomeSeer using the bundled data.
        Dim ddRef As Integer = HomeSeerSystem.CreateDevice(dd)
        Dim dv As HsDevice = HomeSeerSystem.GetDeviceByRef(ddRef)
        dv.Image = "images/evcstat/thermostat-sub.png"
        HomeSeerSystem.UpdateDeviceByRef(dv.Ref, dv.Changes)
        'update the address to device ref cache
        UpdateDeviceReferenceCache()

        'check to see if we need to add additional pages now.
        '        LoadAdditionalPages()
    End Sub
#End Region

#Region "Public Subs/Functions"
    Public Sub Poll(Optional Address As Integer = 255)
        Logger.LogDebug("In Poll: Time: {0}", Now())
        Try
            If Address <> 255 Then
                _commThread.SendCommand("A=" & Address.ToString & " O=00 R=1 R=2 SC=?" & vbCr)
            Else
                For Each i In _addressRefDict
                    _commThread.SendCommand("A=" & i.Key.ToString & " O=00 R=1 R=2 SC=?" & vbCr)
                Next
            End If
        Catch ex As Exception
            Logger.LogError("Error in Poll: {0}", ex.Message)
        End Try

    End Sub

    Public Sub ProcessDataReceived(ByVal Address As Integer, ByVal Data As String)
        Dim dv As HsDevice
        Try
            If _addressRefDict.ContainsKey(Address) Then
                dv = HomeSeerSystem.GetDeviceWithFeaturesByRef(_addressRefDict.Item(Address))
                ProcessDataReceived(dv, Data)
            ElseIf Address = "255" Then
                For Each i In _addressRefDict
                    dv = HomeSeerSystem.GetDeviceWithFeaturesByRef(i.Value)
                    ProcessDataReceived(dv, Data)
                Next
            End If
        Catch ex As Exception
            Logger.LogError("Error in ProcessDataReceived obtaining device from address {0} {1}", Address, ex.Message)
        End Try

    End Sub
#End Region

#Region "Private Subs/Functions"

    Private Sub ProcessDataReceived(ByVal dv As HsDevice, ByVal Data As String)
        Dim Prop As String
        Dim Value As String
        Dim df As HsFeature
        Dim ti As New TypeInfo
        ' OK, We can get multiple items here.  So, split them all then parse each one.
        Dim commands() As String = Split(Trim(Data))
        Try
            For Each item As String In commands
                ' Get The Property 
                Prop = Mid(item, 1, InStr(item, "=") - 1)
                Value = Mid(item, InStr(item, "=") + 1)

                Logger.LogDebug("In ProcessDataReceived, Property= {0}, Value= {1}", Prop, Value)

                Try
                    Select Case Prop
                        Case "A"
                            'Do nothing as we already have the address
                        Case "Z"
                            'Not using zones
                        Case "O"
                            'Not using owner
                        Case "M"
                            ti.Type = EFeatureType.ThermostatControl
                            ti.SubType = EThermostatControlFeatureSubType.ModeSet
                            ti.ApiType = EApiType.Feature
                            df = dv.GetFeatureByType(ti)
                            Select Case Value
                                Case "O"
                                    df.Value = Controls.EControlUse.ThermModeOff
                                Case "A"
                                    df.Value = Controls.EControlUse.ThermModeAuto
                                Case "C"
                                    df.Value = Controls.EControlUse.ThermModeCool
                                Case "H"
                                    df.Value = Controls.EControlUse.ThermModeHeat
                                Case "E", "EH"
                                    'df.Value = Controls.EControlUse.ThermModeAux
                                Case Else
                                    Logger.LogWarning("In ProcessDataReceived, Invalid Mode Value, Property= {0}, Value= {1}", Prop, Value)
                            End Select
                            Try
                                HomeSeerSystem.UpdateFeatureByRef(df.Ref, df.Changes)
                            Catch ex As Exception
                                Logger.LogWarning("Error in ProcessDataReceived, Updating Device, {0} Name {1} Ref {2}", ex.Message, df.Name, df.Ref)
                            End Try
                        Case "F", "FM"
                            ti.Type = EFeatureType.ThermostatControl
                            ti.SubType = EThermostatControlFeatureSubType.FanModeSet
                            ti.ApiType = EApiType.Feature
                            df = dv.GetFeatureByType(ti)
                            Select Case Value
                                Case "A"
                                    df.Value = Controls.EControlUse.ThermFanAuto
                                Case "O"
                                    df.Value = Controls.EControlUse.ThermFanOn
                                Case Else
                                    Logger.LogWarning("In ProcessDataReceived, Invalid Fan Value, Property= {0}, Value= {1}", Prop, Value)
                            End Select
                            Try
                                HomeSeerSystem.UpdateFeatureByRef(df.Ref, df.Changes)
                            Catch ex As Exception
                                Logger.LogWarning("Error in ProcessDataReceived, Updating Device, {0} Name {1} Ref {2}", ex.Message, df.Name, df.Ref)
                            End Try
                        Case "DS"
                            Select Case Value
                                Case "O"
                                    'df.Value = True
                                Case "C"
                                    'df.Value = False
                                Case Else
                                    Logger.LogWarning("In ProcessDataReceived, Invalid Damper Value, Property= {0}, Value= {1}", Prop, Value)
                            End Select
                            'HomeSeerSystem.UpdateFeatureByRef(df.Ref, df.Changes)
                        Case "CS"
                            ti.Type = EFeatureType.ThermostatStatus
                            ti.SubType = EThermostatStatusFeatureSubType.OperatingState
                            ti.ApiType = EApiType.Feature
                            df = dv.GetFeatureByType(ti)
                            Select Case Value
                                Case "C"
                                    df.Value = True
                                Case "I"
                                    df.Value = False
                                Case Else
                                    Logger.LogWarning("In ProcessDataReceived, Invalid Calling Value, Property= {0}, Value= {1}", Prop, Value)
                            End Select
                            Try
                                HomeSeerSystem.UpdateFeatureByRef(df.Ref, df.Changes)
                            Catch ex As Exception
                                Logger.LogWarning("Error in ProcessDataReceived, Updating Device, {0} Name {1} Ref {2}", ex.Message, df.Name, df.Ref)
                            End Try
                        Case "FR"
                            'Remaining Filter Time
                        Case "FT"
                            'Total Filter Time
                        Case "SPH"
                            'HeatSet
                            ti.Type = EFeatureType.ThermostatControl
                            ti.SubType = EThermostatControlFeatureSubType.HeatingSetPoint
                            ti.ApiType = EApiType.Feature
                            df = dv.GetFeatureByType(ti)
                            df.Value = Value
                            Try
                                HomeSeerSystem.UpdateFeatureByRef(df.Ref, df.Changes)
                            Catch ex As Exception
                                Logger.LogWarning("Error in ProcessDataReceived, Updating Device, {0} Name {1} Ref {2}", ex.Message, df.Name, df.Ref)
                            End Try
                        Case "SPC"
                            'Coolset
                            ti.Type = EFeatureType.ThermostatControl
                            ti.SubType = EThermostatControlFeatureSubType.CoolingSetPoint
                            ti.ApiType = EApiType.Feature
                            df = dv.GetFeatureByType(ti)
                            df.Value = Value
                            Try
                                HomeSeerSystem.UpdateFeatureByRef(df.Ref, df.Changes)
                            Catch ex As Exception
                                Logger.LogWarning("Error in ProcessDataReceived, Updating Device, {0} Name {1} Ref {2}", ex.Message, df.Name, df.Ref)
                            End Try
                        Case "T"
                            'Temperature
                            ti.Type = EFeatureType.ThermostatStatus
                            ti.SubType = EThermostatStatusFeatureSubType.Temperature
                            ti.ApiType = EApiType.Feature
                            df = dv.GetFeatureByType(ti)
                            df.Value = Value
                            Try
                                HomeSeerSystem.UpdateFeatureByRef(df.Ref, df.Changes)
                            Catch ex As Exception
                                Logger.LogWarning("Error in ProcessDataReceived, Updating Device, {0} Name {1} Ref {2}", ex.Message, df.Name, df.Ref)
                            End Try
                        Case "TM"
                            'message displayed on thermostat = Value
                        Case "OA"
                            'Other Temperature / Outside Temperature
                            ti.Type = EFeatureType.ThermostatStatus
                            ti.SubType = EThermostatStatusFeatureSubType.TemperatureOther
                            ti.ApiType = EApiType.Feature
                            df = dv.GetFeatureByType(ti)
                            df.Value = Value
                            Try
                                HomeSeerSystem.UpdateFeatureByRef(df.Ref, df.Changes)
                            Catch ex As Exception
                                Logger.LogWarning("Error in ProcessDataReceived, Updating Device, {0} Name {1} Ref {2}", ex.Message, df.Name, df.Ref)
                            End Try
                        Case "SC"
                            ti.Type = EFeatureType.ThermostatControl
                            ti.SubType = EThermostatControlFeatureSubType.HoldMode
                            ti.ApiType = EApiType.Feature
                            df = dv.GetFeatureByType(ti)
                            Select Case Value
                                Case "0"
                                    df.Value = Controls.EControlUse.Off
                                Case "1"
                                    df.Value = Controls.EControlUse.On
                                Case "2"
                                    df.Value = Controls.EControlUse.OnAlternate
                                Case Else
                                    Logger.LogWarning("In ProcessDataReceived, Invalid Hold Value,  Property= {0}, Value= {1}", Prop, Value)
                            End Select
                            Try
                                HomeSeerSystem.UpdateFeatureByRef(df.Ref, df.Changes)
                            Catch ex As Exception
                                Logger.LogWarning("Error in ProcessDataReceived, Updating Device, {0} Name {1} Ref {2}", ex.Message, df.Name, df.Ref)
                            End Try
                        Case "SCH"
                            'SetExtraData(dv.Ref, DeviceConfigScheduleId, Newtonsoft.Json.Linq.JObject.Parse(Value).ToString())
                            SetExtraData(dv.Ref, DeviceConfigScheduleId, JsonConvert.DeserializeObject(Value).ToString())
                            Try
                                HomeSeerSystem.UpdateDeviceByRef(dv.Ref, dv.Changes)
                            Catch ex As Exception
                                Logger.LogWarning("Error in ProcessDataReceived, Updating Device, {0} Name {1} Ref {2}", ex.Message, dv.Name, dv.Ref)
                            End Try
                        Case Else
                            'CheckTriggers(addr, zone, Prop, Value)
                    End Select
                Catch ex As Exception
                    Logger.LogWarning("Error in ProcessDataReceived Select Block, Selecting Properties, {0}", ex.Message)
                End Try
            Next
        Catch ex As Exception
            Logger.LogWarning("Error in ProcessDataReceived, Selecting Properties, {0}", ex.Message)
        End Try

    End Sub

    Private Sub UpdatePhysicalDevice(ByVal Address As Integer, ByVal ControlTypeSetting As EThermostatControlFeatureSubType, ByVal Value As Integer)
        'This is custom based on the manufacturer
        Dim Command As String = ""
        Try
            Select Case ControlTypeSetting
                Case EThermostatControlFeatureSubType.HeatingSetPoint
                    Command = "SPH=" & CStr(Value)
                Case EThermostatControlFeatureSubType.CoolingSetPoint
                    Command = "SPC=" & CStr(Value)
                Case EThermostatControlFeatureSubType.FanModeSet
                    Select Case Value
                        Case Controls.EControlUse.ThermFanAuto
                            Command = "F=A"
                        Case Controls.EControlUse.ThermFanOn
                            Command = "F=O"
                    End Select
                Case EThermostatControlFeatureSubType.HoldMode
                    Select Case Value
                        Case Controls.EControlUse.Off
                            Command = "SC=0"
                        Case Controls.EControlUse.On
                            Command = "SC=1"
                        Case Controls.EControlUse.OnAlternate
                            Command = "SC=2"
                    End Select
                Case EThermostatControlFeatureSubType.ModeSet
                    Select Case Value
                        Case Controls.EControlUse.ThermModeAuto
                            Command = "M=3"
                        Case Controls.EControlUse.ThermModeCool
                            Command = "M=2"
                        Case Controls.EControlUse.ThermModeHeat
                            Command = "M=1"
                        Case Controls.EControlUse.ThermModeOff
                            Command = "M=0"
                    End Select
            End Select
            If Command.Length > 0 Then
                Logger.LogDebug("Sending command {0} to Address {1} Owner {2}", Command, Address, "O=00 ")
                _commThread.SendCommand("A=" & Address.ToString & " " & "O=00 " & Command & vbCr)
            End If

        Catch ex As Exception
            Logger.LogError("Error in UpdatePhysicalDevice, Address {0} Value {1} - {2}", Address, Value, ex.Message.Length)
        End Try
    End Sub

    Private Sub UpdatePhysicalDeviceSchedule(ByVal Address As Integer, ByVal Value As Dictionary(Of String, Dictionary(Of String, Period)))
        'This is custom based on the manufacturer
        Dim Command As String = ""
        Try
            For Each i In Value
                For Each d In i.Value
                    '                   If d.Value.HasChanged Then
                    Dim p As Period = d.Value
                    Command = "SCHP=" + "{""Day"":""" + i.Key.ToString() + """,""Period"":""" + d.Key.ToString() + """,""Time"":""" + p.Time.TimeOfDay.ToString("hh\:mm") + """,""Cool"":" + p.Cool.ToString() + ",""Heat"":" + p.Heat.ToString() + ",""Fan"":" + p.Fan.ToString() + "}"
                    'Command = "SCHP=" + "{" + """i.Key"":" + """d.Key"":" + """Time"":""" + p.Time.TimeOfDay.ToString() + """, ""Cool"":" + p.Cool.ToString() + """Heat"":" + p.Heat.ToString() + ", ""Fan"":" + p.Fan.ToString(") + "}"

                    Logger.LogDebug("Sending command {0} to Address {1} Owner {2}", Command, Address, "O=00 ")
                        _commThread.SendCommand("A=" & Address.ToString & " " & "O=00 " & Command & vbCr)

                    If LogDebug Then
                        Console.WriteLine("Day {0} Period {1} Time Of Day {2}", i.Key, d.Key, p.Time.TimeOfDay)
                        Console.WriteLine("Day {0} Period {1} Cool Set Point {2}", i.Key, d.Key, p.Cool)
                        Console.WriteLine("Day {0} Period {1} Heat Set Point {2}", i.Key, d.Key, p.Heat)
                        Console.WriteLine("Day {0} Period {1} Fan Setting {2}", i.Key, d.Key, p.Fan)
                    End If

                    '                   End If
                Next
            Next

            If Command.Length > 0 Then
            End If

        Catch ex As Exception
            Logger.LogError("Error in UpdatePhysicalDeviceSchedule, Address {0} Value {1} - {2}", Address, Value, ex.Message.Length)
        End Try
    End Sub
    Private Sub UpdateDeviceReferenceCache()
        Dim RefDict As Dictionary(Of Integer, Object)
        RefDict = HomeSeerSystem.GetPropertyByInterface(Id, EProperty.Address, True)

        If _addressRefDict Is Nothing Then
            _addressRefDict = New Dictionary(Of Integer, Integer)
        Else
            _addressRefDict.Clear()
        End If

        For Each i In RefDict
            _addressRefDict.Add(i.Value, i.Key)
        Next

    End Sub
#End Region
End Class