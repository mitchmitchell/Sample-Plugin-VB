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
'''  that corresponds to the name of the executable. For this plugin, "HomeSeerSamplePluginVB" the executable
'''  file is "HSPI_HomeSeerSamplePluginVB.exe" and this class is HSPI_HomeSeerSamplePluginVB.HSPI
''' <para>
''' If HomeSeer is unable to find this class, the plugin will not start.
''' </para>
''' </remarks>
Public Class HSPI
    Inherits AbstractPlugin
    Implements WriteLogSampleActionType.IWriteLogActionListener


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
    '''  ..\Homeseer\Homeseer\html\HomeSeerSamplePluginVB\
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
        ActionTypes.AddActionType(GetType(WriteLogSampleActionType))
        TriggerTypes.AddTriggerType(GetType(SampleTriggerType))
    End Sub

    ''' <summary>
    ''' Initialize the starting state of the settings pages for the HomeSeerSamplePlugin.
    '''  This constructs the framework that the user configurable settings for the plugin live in.
    '''  Any saved configuration options are loaded later in <see cref="Initialize"/> using
    '''  <see cref="AbstractPlugin.LoadSettingsFromIni"/>
    ''' </summary>
    ''' <remarks>
    ''' For ease of use throughout the plugin, all of the view IDs, names, and values (non-volatile data)
    '''  are stored in the <see cref="HSPI_HomeSeerSamplePluginVB.Constants.Settings"/> static class.
    ''' </remarks>
    Private Sub InitializeSettingsPages()
        'Initialize the first settings page
        ' This page is used to manipulate the behavior of the sample plugin

        'Start a PageFactory to construct the Page
        Dim settingsPage1 = PageFactory.CreateSettingsPage(Constants.Settings.SettingsPage1Id, Constants.Settings.SettingsPage1Name)
        'Add a LabelView to the page
        settingsPage1.WithLabel(Constants.Settings.Sp1ColorLabelId, Nothing, Constants.Settings.Sp1ColorLabelValue)
        'Create a group of ToggleViews displayed as a flexbox grid 
        Dim colorViewGroup = New GridView(Constants.Settings.Sp1ColorGroupId, Constants.Settings.Sp1ColorGroupName)
        Dim colorFirstRow = New GridRow()
        colorFirstRow.AddItem(New ToggleView(Constants.Settings.Sp1ColorToggleRedId, Constants.Settings.ColorRedName, True) With {
            .ToggleType = EToggleType.Checkbox
        }, extraSmallSize:=EColSize.Col6, largeSize:=EColSize.Col3)
        colorFirstRow.AddItem(New ToggleView(Constants.Settings.Sp1ColorToggleOrangeId, Constants.Settings.ColorOrangeName, True) With {
            .ToggleType = EToggleType.Checkbox
        }, extraSmallSize:=EColSize.Col6, largeSize:=EColSize.Col3)
        colorFirstRow.AddItem(New ToggleView(Constants.Settings.Sp1ColorToggleYellowId, Constants.Settings.ColorYellowName, True) With {
            .ToggleType = EToggleType.Checkbox
        }, extraSmallSize:=EColSize.Col6, largeSize:=EColSize.Col3)
        colorFirstRow.AddItem(New ToggleView(Constants.Settings.Sp1ColorToggleGreenId, Constants.Settings.ColorGreenName, True) With {
            .ToggleType = EToggleType.Checkbox
        }, extraSmallSize:=EColSize.Col6, largeSize:=EColSize.Col3)
        Dim colorSecondRow = New GridRow()
        colorSecondRow.AddItem(New ToggleView(Constants.Settings.Sp1ColorToggleBlueId, Constants.Settings.ColorBlueName, True) With {
            .ToggleType = EToggleType.Checkbox
        }, extraSmallSize:=EColSize.Col6, largeSize:=EColSize.Col3)
        colorSecondRow.AddItem(New ToggleView(Constants.Settings.Sp1ColorToggleIndigoId, Constants.Settings.ColorIndigoName, True) With {
            .ToggleType = EToggleType.Checkbox
        }, extraSmallSize:=EColSize.Col6, largeSize:=EColSize.Col3)
        colorSecondRow.AddItem(New ToggleView(Constants.Settings.Sp1ColorToggleVioletId, Constants.Settings.ColorVioletName, True) With {
            .ToggleType = EToggleType.Checkbox
        }, extraSmallSize:=EColSize.Col6, largeSize:=EColSize.Col3)
        colorViewGroup.AddRow(colorFirstRow)
        colorViewGroup.AddRow(colorSecondRow)
        'Add the GridView containing all of the ToggleViews to the page
        settingsPage1.WithView(colorViewGroup)
        'Create 2 ToggleViews for controlling the visibility of the other two settings pages
        Dim pageToggles = New List(Of ToggleView) From {
            New ToggleView(Constants.Settings.Sp1PageVisToggle1Id, Constants.Settings.Sp1PageVisToggle1Name, True),
            New ToggleView(Constants.Settings.Sp1PageVisToggle2Id, Constants.Settings.Sp1PageVisToggle2Name, True)
        }
        'Add a ViewGroup containing all of the ToggleViews to the page
        settingsPage1.WithGroup(Constants.Settings.Sp1PageToggleGroupId, Constants.Settings.Sp1PageToggleGroupName, pageToggles)
        'Add the first page to the list of plugin settings pages
        Settings.Add(settingsPage1.Page)

        'Initialize the second settings page
        ' This page is used to visually demonstrate all of the available JUI views except for InputViews.
        ' None of these views interact with the plugin and are merely for show.

        'Start a PageFactory to construct the Page
        Dim settingsPage2 = PageFactory.CreateSettingsPage(Constants.Settings.SettingsPage2Id, Constants.Settings.SettingsPage2Name)
        'Add a LabelView with a title to the page
        settingsPage2.WithLabel(Constants.Settings.Sp2LabelWTitleId, Constants.Settings.Sp2LabelWTitleName, Constants.Settings.Sp2LabelWTitleValue)
        'Add a LabelView without a title to the page
        settingsPage2.WithLabel(Constants.Settings.Sp2LabelWoTitleId, Nothing, Constants.Settings.Sp2LabelWoTitleValue)
        'Add a toggle switch to the page
        settingsPage2.WithToggle(Constants.Settings.Sp2SampleToggleId, Constants.Settings.Sp2SampleToggleName)
        'Add a checkbox to the page
        settingsPage2.WithCheckBox(Constants.Settings.Sp2SampleCheckBoxId, Constants.Settings.Sp2SampleCheckBoxName)
        'Add a drop down select list to the page
        settingsPage2.WithDropDownSelectList(Constants.Settings.Sp2SelectListId, Constants.Settings.Sp2SelectListName, Constants.Settings.Sp2SelectListOptions)
        'Add a radio select list to the page
        settingsPage2.WithRadioSelectList(Constants.Settings.Sp2RadioSlId, Constants.Settings.Sp2RadioSlName, Constants.Settings.Sp2SelectListOptions)
        'Add a text area to the page
        settingsPage2.WithTextArea(Constants.Settings.Sp2TextAreaId, Constants.Settings.Sp2TextAreaName, 3)
        'Add a time span to the page
        settingsPage2.WithTimeSpan(Constants.Settings.Sp2SampleTimeSpanId, Constants.Settings.Sp2SampleTimeSpanName)
        'Add the second page to the list of plugin settings pages
        Settings.Add(settingsPage2.Page)

        'Initialize the third settings page
        ' This page is used to visually demonstrate the different types of JUI InputViews.

        'Start a PageFactory to construct the Page
        Dim settingsPage3 = PageFactory.CreateSettingsPage(Constants.Settings.SettingsPage3Id, Constants.Settings.SettingsPage3Name)
        'Add a text InputView to the page
        settingsPage3.WithInput(Constants.Settings.Sp3SampleInput1Id, Constants.Settings.Sp3SampleInput1Name)
        'Add a number InputView to the page
        settingsPage3.WithInput(Constants.Settings.Sp3SampleInput2Id, Constants.Settings.Sp3SampleInput2Name, EInputType.Number)
        'Add an email InputView to the page
        settingsPage3.WithInput(Constants.Settings.Sp3SampleInput3Id, Constants.Settings.Sp3SampleInput3Name, EInputType.Email)
        'Add a URL InputView to the page
        settingsPage3.WithInput(Constants.Settings.Sp3SampleInput4Id, Constants.Settings.Sp3SampleInput4Name, EInputType.Url)
        'Add a password InputView to the page
        settingsPage3.WithInput(Constants.Settings.Sp3SampleInput5Id, Constants.Settings.Sp3SampleInput5Name, EInputType.Password)
        'Add a decimal InputView to the page
        settingsPage3.WithInput(Constants.Settings.Sp3SampleInput6Id, Constants.Settings.Sp3SampleInput6Name, EInputType.Decimal)
        'Add the third page to the list of plugin settings pages
        Settings.Add(settingsPage3.Page)
    End Sub

    Protected Overrides Sub Initialize()
        'Load the state of Settings saved to INI if there are any.
        LoadSettingsFromIni()
        If LogDebug Then
            Logger.LogDebug("Registering feature pages")
        End If
        'Initialize feature pages
        HomeSeerSystem.RegisterFeaturePage(Id, "sample-guided-process.html", "Sample Guided Process")
        HomeSeerSystem.RegisterFeaturePage(Id, "sample-blank.html", "Sample Blank Page")
        HomeSeerSystem.RegisterFeaturePage(Id, "sample-trigger-feature.html", "Trigger Feature Page")
        HomeSeerSystem.RegisterFeaturePage(Id, "sample-functions.html", "Plugin Functions Sample")
        HomeSeerSystem.RegisterDeviceIncPage(Id, "add-sample-device.html", "Add Sample Device")

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
        _commThread.Start(HomeSeerSystem.GetIpAddress)
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
        Logger.LogDebug("Shutting down")
        _speakerClient.Disconnect()
    End Sub

    Protected Overrides Function OnSettingChange(pageId As String, currentView As AbstractView, changedView As AbstractView) As Boolean

        'React to the toggles that control the visibility of the last 2 settings pages
        If changedView.Id = Constants.Settings.Sp1PageVisToggle1Id Then
            'Make sure the changed view is a ToggleView
            Dim tView As ToggleView = TryCast(changedView, ToggleView)
            If tView Is Nothing Then
                Return False
            End If

            'Show/Hide the second page based on the new state of the toggle
            If tView.IsEnabled Then
                Settings.ShowPageById(Constants.Settings.SettingsPage2Id)
            Else
                Settings.HidePageById(Constants.Settings.SettingsPage2Id)
            End If
        ElseIf changedView.Id = Constants.Settings.Sp1PageVisToggle2Id Then
            'Make sure the changed view is a ToggleView
            Dim tView As ToggleView = TryCast(changedView, ToggleView)
            If tView Is Nothing Then
                Return False
            End If

            'Show/Hide the second page based on the new state of the toggle
            If tView.IsEnabled Then
                Settings.ShowPageById(Constants.Settings.SettingsPage3Id)
            Else
                Settings.HidePageById(Constants.Settings.SettingsPage3Id)
            End If
        Else
            If LogDebug Then
                Console.WriteLine($"View ID {changedView.Id} does not match any views on the page.")
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

        Dim textAreaSavedValue As String = GetExtraData(deviceRef, DeviceConfigTextAreaId)
        Dim textAreaValue As String = ""

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

            Case "sample-guided-process.html"

                'Handle the Guided Process page
                Try
                    Dim postData = JsonConvert.DeserializeObject(Of SampleGuidedProcessData)(data)
                    If LogDebug Then
                        Console.WriteLine("Post back from sample-guided-process page")
                    End If
                    response = postData.GetResponse()
                Catch exception As JsonSerializationException
                    If LogDebug Then
                        Console.WriteLine(exception.Message)
                    End If
                    response = "error"
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
    ''' Called by the sample guided process feature page through a liquid tag to provide the list of available colors
    ''' <para>
    ''' {{plugin_function 'HomeSeerSamplePluginVB' 'GetSampleSelectList' []}}
    ''' </para>
    ''' </summary>
    ''' <returns>The HTML for the list of select list options</returns>
    Public Function GetSampleSelectList() As String
        If LogDebug Then
            Console.WriteLine("Getting sample select list for sample-guided-process page")
        End If
        Dim sb = New StringBuilder("<select class=""mdb-select md-form"" id=""step3SampleSelectList"">")
        sb.Append(Environment.NewLine)
        sb.Append("<option value="""" disabled selected>Color</option>")
        sb.Append(Environment.NewLine)
        Dim colorList = New List(Of String)()


        Try
            Dim colorSettings = Settings(Constants.Settings.SettingsPage1Id).GetViewById(Constants.Settings.Sp1ColorGroupId)
            Dim colorViewGroup As ViewGroup = TryCast(colorSettings, ViewGroup)
            Dim colorView As ToggleView

            If colorViewGroup Is Nothing Then
                Throw New ViewTypeMismatchException("No View Group found containing colors")
            End If

            For Each view In colorViewGroup.Views

                colorView = TryCast(view, ToggleView)
                If colorView Is Nothing Then
                    Continue For
                End If

                colorList.Add(If(colorView.IsEnabled, colorView.Name, ""))
            Next

        Catch exception As Exception
            If LogDebug Then
                Console.WriteLine(exception)
            End If
            colorList = Constants.Settings.ColorMap.Values.ToList()
        End Try

        For i = 0 To colorList.Count - 1
            Dim color = colorList(i)

            If String.IsNullOrEmpty(color) Then
                Continue For
            End If

            sb.Append("<option value=""")
            sb.Append(i)
            sb.Append(""">")
            sb.Append(color)
            sb.Append("</option>")
            sb.Append(Environment.NewLine)
        Next

        sb.Append("</select>")
        Return sb.ToString()
    End Function

    ''' <summary>
    ''' Called by the sample trigger feature page to get the HTML for a list of checkboxes to use a trigger options
    ''' <para>
    ''' {{list=plugin_function 'HomeSeerSamplePluginVB' 'GetTriggerOptionsHtml' [2]}}
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
    ''' {{list2=plugin_function 'HomeSeerSamplePluginVB' 'GetTriggerOptions' [2]}}
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
    Public Sub WriteLog(ByVal logType As ELogType, ByVal message As String) Implements WriteLogSampleActionType.IWriteLogActionListener.WriteLog
        HomeSeerSystem.WriteLog(logType, message, Name)
    End Sub

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

    ' custom functions that can be accessed from a feature page
    <Serializable>
    Public Class CustomClass
        Public IntItem As Integer
        Public StringItem As String
        Public ArrayItem As New List(Of String)
    End Class

    Public Function MyCustomFunctionArray(param As String) As List(Of CustomClass)
        Dim list As New List(Of CustomClass)
        Dim cc As CustomClass
        Dim ai As List(Of String)

        cc = New CustomClass
        cc.IntItem = 1
        cc.StringItem = "string 1"
        ai = New List(Of String)
        ai.Add("list item 1")
        ai.Add("list item 2")
        cc.ArrayItem = ai
        list.Add(cc)

        cc = New CustomClass
        cc.IntItem = 2
        cc.StringItem = "string 2"
        ai = New List(Of String)
        ai.Add("list item 3")
        ai.Add("list item 4")
        cc.ArrayItem = ai
        list.Add(cc)
        Return list
    End Function


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
        tr = New ValueRange(0.0, 120.0)
        tr.Suffix = "°"
        Dim sg = New StatusGraphic("images/evcstat/thermostat-sub.png", tr)
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


        'Add the feature data to the device data in the device factory
        df.WithFeature(ff)

        'Put device specific data with the device. (this is the cameradata class)
        Dim PED As New HomeSeer.PluginSdk.Devices.PlugExtraData
        PED.AddNamed("schedule", Newtonsoft.Json.Linq.JObject.Parse("{""period"":[{""time"":570,""heat"":74,""cool"":78,""fan"":4},{""time"":2047,""heat"":62,""cool"":85,""fan"":4},{""time"":2047,""heat"":70,""cool"":78,""fan"":4},{""time"":1380,""heat"":74,""cool"":78,""fan"":4}]}").ToString())
        df.WithExtraData(PED)

        'this bundles all the needed data from the device to send to HomeSeer.
        dd = df.PrepareForHs

        'this creates the device in HomeSeer using the bundled data.
        HomeSeerSystem.CreateDevice(dd)
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

    Public Sub ProcessDataReceived(ByVal dv As HsDevice, ByVal Data As String)
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
                            dv.PlugExtraData.Item("schedule") = Newtonsoft.Json.Linq.JObject.Parse(Value)
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

    Public Sub UpdatePhysicalDevice(ByVal Address As Integer, ByVal ControlTypeSetting As EThermostatControlFeatureSubType, ByVal Value As Integer)
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
                _commThread.SendCommand("A=" & Address.ToString & " " & "O=00 " & Command & vbCr)
            End If

        Catch ex As Exception
            Logger.LogError("Error in UpdatePhysicalDevice, Address {0} Value {1} - {2}", Address, Value, ex.Message.Length)
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