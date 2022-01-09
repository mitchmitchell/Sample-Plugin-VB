Namespace Constants
    Public Module Settings

        Public Const SettingsPageId As String = "settings-page"
        Public Const SettingsPageName As String = "Thermostat Plugin Settings"

        Public ReadOnly Property SpViewGroupId As String
            Get
                Return $"{SettingsPageId}-mqtt-configuration"
            End Get
        End Property

        Public Const SpViewGroupName As String = "Thermostat MQTT Configuration"

        Public ReadOnly Property SpHostAddressId As String
            Get
                Return $"{SettingsPageId}-mqtt-host-address"
            End Get
        End Property

        Public Const SpHostAddressName As String = "MQTT Host Address"
        Public Const SpHostAddressDefault As String = "127.0.0.1"

        Public ReadOnly Property SpSendTopicId As String
            Get
                Return $"{SettingsPageId}-mqtt-send-topic"
            End Get
        End Property

        Public Const SpSendTopicName As String = "MQTT Send Topic"
        Public Const SpSendTopicDefault As String = "homeseer/evc/out"

        Public ReadOnly Property SpReceiveTopicId As String
            Get
                Return $"{SettingsPageId}-mqtt-receive-topic"
            End Get
        End Property

        Public Const SpReceiveTopicName As String = "MQTT Receive Topic"
        Public Const SpReceiveTopicDefault As String = "homeseer/evc/in"

        Public ReadOnly Property SpPollIntervalId As String
            Get
                Return $"{SettingsPageId}-thermostat-poll-interval"
            End Get
        End Property

        Public Const SpPollIntervalName As String = "Thermostat Poll Interval"
        Public Const SpPollIntervalDefault As String = "0"

        Public ReadOnly Property SpDebugToggleGroupId As String
            Get
                Return $"{SettingsPageId}-debugtogglegroup"
            End Get
        End Property

        Public Const SpDebugToggleGroupName As String = "Toggle Debug Logging on or off"

        Public ReadOnly Property SpDebugEnableToggleId As String
            Get
                Return $"{SettingsPageId}-debugtoggle"
            End Get
        End Property

        Public Const SpDebugEnableToggleName As String = "Debug Logging"

    End Module
End Namespace