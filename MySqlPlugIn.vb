﻿Imports JHSoftware.SimpleDNS.Plugin
Imports MySql.Data.MySqlClient

Public Class MySqlPlugIn
  Implements ILookupHost
  Implements ILookupReverse
  Implements IOptionsUI

  Dim cfg As MyConfig

  Public Property Host As IHost Implements IPlugInBase.Host

  Public Function InstanceConflict(ByVal configXML1 As String, ByVal configXML2 As String, ByRef errorMsg As String) As Boolean Implements JHSoftware.SimpleDNS.Plugin.IPlugInBase.InstanceConflict
    Return False
  End Function

#Region "Methods"

  Public Function GetPlugInTypeInfo() As TypeInfo Implements JHSoftware.SimpleDNS.Plugin.IPlugInBase.GetTypeInfo
    With GetPlugInTypeInfo
      .Name = "MySQL Server"
      .Description = "Fetches host records from a MySQL server"
      .InfoURL = "https://simpledns.plus/plugin-mysql"
    End With
  End Function

  Public Sub LoadConfig(ByVal config As String, ByVal instanceID As Guid, ByVal dataPath As String) Implements JHSoftware.SimpleDNS.Plugin.IPlugInBase.LoadConfig
    cfg = MyConfig.Load(config)
  End Sub

  Public Function StartService() As Threading.Tasks.Task Implements JHSoftware.SimpleDNS.Plugin.IPlugInBase.StartService
    Return Threading.Tasks.Task.CompletedTask
  End Function

  Public Sub StopService() Implements JHSoftware.SimpleDNS.Plugin.IPlugInBase.StopService
    ' If dbConn IsNot Nothing Then dbConn.Close() : dbConn = Nothing
  End Sub

  Private Async Function LookupHost(name As DomName, ipv6 As Boolean, req As IRequestContext) As Threading.Tasks.Task(Of LookupResult(Of SdnsIP)) Implements JHSoftware.SimpleDNS.Plugin.ILookupHost.LookupHost
    Using dbConn = New MySqlConnection(cfg.dbConnStr)
      Await dbConn.OpenAsync()
      Dim selStr = If(ipv6, cfg.SelectFwd6, cfg.SelectFwd4)
      If String.IsNullOrEmpty(selStr) Then Return Nothing
      Dim cmd = dbConn.CreateCommand
      cmd.CommandText = selStr
      cmd.Parameters.AddWithValue("?hostname", name.ToString())
      If selStr.IndexOf("?clientip") >= 0 Then cmd.Parameters.AddWithValue("?clientip", req.FromIP.ToString)
      Dim rdr = Await cmd.ExecuteReaderAsync
      If Not Await rdr.ReadAsync Then rdr.Close() : Return Nothing
      Dim rv = New LookupResult(Of SdnsIP) With {.Value = SdnsIP.Parse(CStr(rdr(0))), .TTL = CInt(rdr(1))}
      rdr.Close()
      If rv.Value.IsIPv6 <> ipv6 Then Return Nothing
      Return rv
    End Using
  End Function

  Public Async Function LookupReverse(ip As SdnsIP, req As IRequestContext) As Threading.Tasks.Task(Of LookupResult(Of DomName)) Implements JHSoftware.SimpleDNS.Plugin.ILookupReverse.LookupReverse
    Using dbConn = New MySqlConnection(cfg.dbConnStr)
      Await dbConn.OpenAsync()
      Dim selStr = If(ip.IsIPv4, cfg.SelectRev4, cfg.SelectRev6)
      If String.IsNullOrEmpty(selStr) Then Return Nothing
      Dim cmd = dbConn.CreateCommand
      cmd.CommandText = selStr
      cmd.Parameters.AddWithValue("?ipaddress", req.QNameIP.ToString)
      If selStr.IndexOf("?clientip") >= 0 Then cmd.Parameters.AddWithValue("?clientip", req.FromIP.ToString)
      Dim rdr = Await cmd.ExecuteReaderAsync
      If Not Await rdr.ReadAsync Then rdr.Close() : Return Nothing
      Dim rv = New LookupResult(Of DomName) With {.Value = DomName.Parse(CStr(rdr(0))), .TTL = CInt(rdr(1))}
      rdr.Close()
      Return rv
    End Using
  End Function

  Public Function GetOptionsUI(ByVal instanceID As Guid, ByVal dataPath As String) As JHSoftware.SimpleDNS.Plugin.OptionsUI Implements JHSoftware.SimpleDNS.Plugin.IOptionsUI.GetOptionsUI
    Return New OptionsCtrl
  End Function

#End Region


End Class
