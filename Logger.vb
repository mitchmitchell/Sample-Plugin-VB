Imports HomeSeer.PluginSdk.Logging

Module Logger
    Sub Log(ByVal line As String, ByVal level As ELogType)
        Try
            Program._plugin.WriteLog(level, line)
        Catch ex As Exception
            Console.WriteLine(ex.ToString())
        End Try
    End Sub

    Sub LogDebug(ByVal line As String)
        Log(line, ELogType.Debug)
    End Sub

    Sub LogDebug(ByVal format As String, ParamArray args As Object())
        LogDebug(String.Format(format, args))
    End Sub

    Sub LogInfo(ByVal line As String)
        Log(line, ELogType.Info)
    End Sub

    Sub LogInfo(ByVal format As String, ParamArray args As Object())
        LogInfo(String.Format(format, args))
    End Sub

    Sub LogWarning(ByVal line As String)
        Log(line, ELogType.Warning)
    End Sub

    Sub LogWarning(ByVal format As String, ParamArray args As Object())
        LogWarning(String.Format(format, args))
    End Sub

    Sub LogError(ByVal line As String)
        Log(line, ELogType.[Error])
    End Sub

    Sub LogError(ByVal format As String, ParamArray args As Object())
        LogError(String.Format(format, args))
    End Sub
End Module

