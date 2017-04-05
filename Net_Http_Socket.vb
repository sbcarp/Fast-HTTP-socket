Imports System
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.IO
Imports System.Security.Cryptography.X509Certificates
Imports System.Net.Security
Imports System.Text.RegularExpressions
Public Class Net_Http_Socket
        Private Const Http_Receive_Buff_Size As Integer = 65536
        Private Const Http_Send_Buff_Size As Integer = 65536
        Private Timer As New Time_Span_Caculater
        Private Socket_Connect_Keep_Timeout As Integer = 15000
        Private Http_Header_Separator() As Byte = {13, 10, 13, 10}
        Private Http_Header_Vbcrlf() As Byte = {13, 10}
        Private Http_Header_Content_Length_Separator() As Byte = {13, 10, 67, 111, 110, 116, 101, 110, 116, 45, 76, 101, 110, 103, 116, 104, 58}
        Private Http_Header_Transfer_Encoding_Chunked_Separator() As Byte = {13, 10, 84, 114, 97, 110, 115, 102, 101, 114, 45, 69, 110, 99, 111, 100, 105, 110, 103, 58, 32, 99, 104, 117, 110, 107, 101, 100, 13, 10}
        Private Http_Header_Transfer_Encoding_Chunked_Separator2() As Byte = {13, 10, 84, 114, 97, 110, 115, 102, 101, 114, 45, 101, 110, 99, 111, 100, 105, 110, 103, 58, 32, 99, 104, 117, 110, 107, 101, 100, 13, 10}
        Private Http_Header_Content_Length As Integer
        Private Http_Header_Separator_Position As Integer
        Private Http_Headers_Separate() As Byte
        Private Http_Content_Separate() As Byte
        Private Http_Content_Chunked_Receive_Status As Integer
        Private Http_Content_Chunked_Single() As Byte
        Private Http_Content_Chunked_Single_Size As Integer
        Private Http_Content_Chunked_Total() As Byte
        Private Http_Content_Chunked_Indicator_Records As New ArrayList
        Private Http_Content_Chunked_Indicator As Integer
        Private Http_Request_IpAndPoint As IPEndPoint
        Private Http_Request_Headers() As String
        Private Http_Request_Host As String
        Private Http_Request_Data() As Byte
        Private Http_Request_Current_Position As Long
        Private Http_Request_Close_Signal As Integer
        Private Http_Send_Count_Total As Long
        Private Http_Send_Stop_Signal As Integer
        Private Http_Receive_Data() As Byte
        Private Http_Receive_Is_First_Packet As Boolean
        Private Http_Receive_Packet_Null_Consecutive_Count As Integer
        Private Http_Receive_Count_Headers As Long
        Private Http_Receive_Count_Total As Long
        Private Http_Receive_Buff(Http_Receive_Buff_Size) As Byte
        Private Http_Request_Protocol As Http_Request_Protocol_Type
        Private Https_Stream_Client As NetworkStream
        Private Https_Ssl_Stream_Client As SslStream
        Private Https_Receive_Count_Once As Integer
        Private Setting_Win_Socket_Is_Close As Boolean
        Private Setting_Timeout_Ssl_Stream_Authenticate_Async As Integer = 3000
        Private Setting_Timeout_Host_Connect As Integer = 20000
        Private Setting_Timeout_Ssl_Stream_Write_Async As Integer = 3000
        Private Setting_Timeout_Ssl_Stream_Read_Async As Integer = 3000
        Private Setting_Timeout_Host_Get_Addresses_Async = 20000
        Private Setting_Timeout_Data_Send = 5000
        Private Setting_Timeout_Data_Receive = 10000
        Private Http_Proxy_Enable As Boolean
        Private Http_Proxy_Address As String
        Private Http_Proxy_Port As Integer
        'Private Http_Proxy_Username As Boolean
        'Private Http_Proxy_Password As Boolean
        'Private Setting_Timeout_ As Integer =
        '    Private Setting_Timeout_ As Integer =
        '    Private Setting_Timeout_ As Integer =
        '    Private Setting_Timeout_ As Integer =
        '    Private Setting_Timeout_ As Integer =
        '    Private Setting_Timeout_ As Integer =
        '    Private Setting_Timeout_ As Integer =
        '    Private Setting_Timeout_ As Integer =
        '    Private Setting_Timeout_ As Integer =
        Public Enum Socket_Error_Type
            Error_Null = -1
            Successd = 0
            Unknown_Protocol = 1
            Faild_Find_Host = 2
            Faild_Start_Connect = 3
            Faild_Start_Send = 4
            Faild_Continu_Send = 5
            Faild_Start_Receive = 6
            Faild_Continu_Receive = 7
            Disconnect_By_Host = 8
            Authenticate_Timeout = 9
            Host_Connect_Timeout = 10
            Ssl_Stream_Write_Timeout = 11
            Ssl_Stream_Read_Timeout = 12
            Host_Get_Addresses_Timeout = 13
            Faild_Receive_Data = 14
            Received_Data_Corrupted = 15
            Data_Send_Timeout = 16
            Data_Receive_Timeout = 17
            DataStructure_Error_Handshake_Failed = 18
            Connection_Refused_By_Host = 19
            Host_Connect_OperationAborted = 20
            Host_Connect_Others = 21
            Other_Error = 99
        End Enum
        Private Manager_Http_Socket_Ref As Manager_Http_Socket
        Enum Http_Request_Protocol_Type
            Http = 0
            Https = 1
        End Enum
        Private Socket_Connect_EventArgs As New SocketAsyncEventArgs
        Private Socket_Send_EventArgs As New SocketAsyncEventArgs
        Private Socket_Receive_EventArgs As New SocketAsyncEventArgs
        Private Socket_Last_Error As Socket_Error_Type
        Private Win_Socket As New Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        Public Sub Socket_Close()
            With Win_Socket
                .Close()
                .Dispose()
            End With
            Win_Socket = New Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            Https_Stream_Client = Nothing
            Https_Ssl_Stream_Client = Nothing
            Socket_Connect_EventArgs.Dispose()
            Socket_Send_EventArgs.Dispose()
            Socket_Receive_EventArgs.Dispose()
            Socket_Connect_EventArgs = New SocketAsyncEventArgs
            Socket_Send_EventArgs = New SocketAsyncEventArgs
            Socket_Receive_EventArgs = New SocketAsyncEventArgs
        End Sub
        Private Sub Socket_Compelete_Request(Socket_Error As Socket_Error_Type, Http_Headers_Data() As Byte, Http_Receive_Data_Sub() As Byte)
            'Win_Socket.Disconnect(False)
            'Win_Socket.Close()
            Http_Request_Close_Signal = 1
            Http_Send_Stop_Signal = 1
            RemoveHandler Socket_Connect_EventArgs.Completed, AddressOf Socket_Connect_EventArgs_Completed
            RemoveHandler Socket_Send_EventArgs.Completed, AddressOf Socket_Send_EventArgs_Completed
            RemoveHandler Socket_Receive_EventArgs.Completed, AddressOf Socket_Receive_EventArgs_Completed
            'Socket_Connect_EventArgs = Nothing
            'Socket_Send_EventArgs = Nothing
            'Socket_Receive_EventArgs = Nothing
            'Win_Socket.Dispose()
            'Win_Socket = Nothing
            Http_Header_Content_Length = 0
            Http_Header_Separator_Position = -1
            Http_Headers_Separate = Nothing
            Http_Content_Separate = Nothing
            Http_Content_Chunked_Receive_Status = 0
            Http_Content_Chunked_Single = Nothing
            Http_Content_Chunked_Single_Size = 0
            Http_Content_Chunked_Total = Nothing
            Http_Request_Headers = Nothing
            Http_Request_Host = ""
            Http_Request_Data = Nothing
            Http_Receive_Is_First_Packet = True
            Http_Receive_Packet_Null_Consecutive_Count = 0
            Http_Request_Current_Position = 0
            'Http_Request_IpAndPoint = Nothing
            Http_Receive_Data = Nothing
            Http_Receive_Count_Headers = 0
            Http_Receive_Count_Total = 0
            Http_Receive_Buff = Nothing
            Http_Content_Chunked_Indicator_Records = Nothing
            Http_Content_Chunked_Indicator = 0
            Https_Receive_Count_Once = 0
            'Https_Stream_Client = Nothing
            'Https_Ssl_Stream_Client = Nothing
            Select Case Socket_Error
                Case Socket_Error_Type.Successd
                Case Else
                    Socket_Close()
            End Select
            If Setting_Win_Socket_Is_Close Then Socket_Close()
            Manager_Http_Socket_Ref.Http_Request_Complete(Socket_Error, Http_Headers_Data, Http_Receive_Data_Sub)
        End Sub
        Private Sub Host_Get_From_Domain(ByVal Domain_Name As String, ByVal Port As Integer)
            Try
                Dim Host_Ip_List As IPAddress()
                Dim Host_Get_Addresses_Async As Threading.Tasks.Task(Of IPAddress())
                Host_Get_Addresses_Async = Dns.GetHostAddressesAsync(Domain_Name)
                With Host_Get_Addresses_Async
                    .Wait(Setting_Timeout_Host_Get_Addresses_Async)
                    If .IsCompleted Then
                        Host_Ip_List = .GetAwaiter.GetResult()
                        Manager_Http_Socket_Ref.Ip_Set_From_Ip_Collection(Domain_Name, Host_Ip_List(0).ToString)
                        Host_Connect(Host_Ip_List(0).ToString, Port)
                    Else
                        Socket_Compelete_Request(Socket_Error_Type.Host_Get_Addresses_Timeout, Nothing, Nothing)
                        Exit Sub
                    End If
                End With
            Catch ex As Exception
                Select Case ex.HResult
                    Case -2146233088
                        Socket_Compelete_Request(Socket_Error_Type.Faild_Find_Host, Nothing, Nothing)
                    Case Else
                        Socket_Compelete_Request(Socket_Error_Type.Other_Error, Nothing, Nothing)
                End Select
            End Try

        End Sub
        Private Sub Host_Connect(ByVal Ip_Address As String, ByVal Port As Integer)
            Try
                If Http_Proxy_Enable Then
                    Ip_Address = Http_Proxy_Address : Port = Http_Proxy_Port
                End If
                Select Case Http_Request_Protocol
                    Case Http_Request_Protocol_Type.Http
                        If Port = 0 Then Port = 80
                        Http_Request_IpAndPoint = New IPEndPoint(Net.IPAddress.Parse(Ip_Address), Port)
                    Case Http_Request_Protocol_Type.Https
                        If Port = 0 Then Port = 443
                        Http_Request_IpAndPoint = New IPEndPoint(Net.IPAddress.Parse(Ip_Address), Port)
                End Select
                If Win_Socket.Connected = True Then
                    Socket_Connect_EventArgs_Completed(Me, Socket_Connect_EventArgs)
                Else
                    With Socket_Connect_EventArgs
                        .AcceptSocket = Win_Socket
                        .RemoteEndPoint = Http_Request_IpAndPoint
                        .SocketError = SocketError.SocketError
                    End With
                    Threading.ThreadPool.QueueUserWorkItem(AddressOf Moniter_Host_Connect_Timeout, Socket_Connect_EventArgs)
                    If Win_Socket.ConnectAsync(Socket_Connect_EventArgs) = False Then
                        Socket_Compelete_Request(Socket_Error_Type.Faild_Start_Connect, Nothing, Nothing)
                        Exit Sub
                    End If
                End If
            Catch ex As Exception
                Select Case ex.HResult
                    Case Else
                        Socket_Compelete_Request(Socket_Error_Type.Other_Error, Nothing, Nothing)
                End Select
            End Try
        End Sub
        Private Sub Moniter_Host_Connect_Timeout(ByVal e As SocketAsyncEventArgs)
            Dim Time_Count As Integer = 0
            Dim Time_Gap As Integer = 20
            Do
                If e.SocketError = SocketError.Success Then
                    Exit Sub
                ElseIf e.SocketError = SocketError.SocketError Then
                    Time_Count += Time_Gap
                Else
                    Exit Sub
                End If
                If Http_Request_Close_Signal = 1 Then Exit Sub
            Delay(Time_Gap)
        Loop Until Time_Count >= Setting_Timeout_Host_Connect
            Socket_Compelete_Request(Socket_Error_Type.Host_Connect_Timeout, Nothing, Nothing)
        End Sub
        Private Sub Moniter_Data_Send_Timeout()
            Dim Time_Count As Integer = 0
            Dim Time_Gap As Integer = 20
            Dim Old_Send_Data_Count As Integer = 0
            Do
                If Http_Receive_Count_Total <> Old_Send_Data_Count Then
                    Old_Send_Data_Count = Http_Receive_Count_Total
                    Time_Count = 0
                Else
                    Time_Count += Time_Gap
                End If
                If Http_Send_Stop_Signal = 1 Then Exit Sub
                If Http_Request_Close_Signal = 1 Then Exit Sub
            Delay(Time_Gap)
        Loop Until Time_Count >= Setting_Timeout_Data_Send
            Socket_Compelete_Request(Socket_Error_Type.Data_Send_Timeout, Nothing, Nothing)
        End Sub
        Private Sub Moniter_Data_Receive_Timeout()
            Dim Time_Count As Integer = 0
            Dim Time_Gap As Integer = 20
            Dim Old_Receive_Data_Count As Integer = 0
            Do
                If Http_Receive_Count_Total <> Old_Receive_Data_Count Then
                    Old_Receive_Data_Count = Http_Receive_Count_Total
                    Time_Count = 0
                Else
                    Time_Count += Time_Gap
                End If
                If Http_Request_Close_Signal = 1 Then Exit Sub
            Delay(Time_Gap)
        Loop Until Time_Count >= Setting_Timeout_Data_Receive
            Socket_Compelete_Request(Socket_Error_Type.Data_Receive_Timeout, Nothing, Nothing)
        End Sub
        Private Sub Socket_Connect_EventArgs_Completed(ByVal sender As Object, ByVal e As SocketAsyncEventArgs)
            Try
                Select Case e.SocketError
                    Case SocketError.OperationAborted
                        Socket_Compelete_Request(Socket_Error_Type.Host_Connect_OperationAborted, Nothing, Nothing)
                        Exit Sub
                    Case SocketError.Success
                    Case SocketError.ConnectionRefused
                        Socket_Compelete_Request(Socket_Error_Type.Connection_Refused_By_Host, Nothing, Nothing)
                        Exit Sub
                    Case SocketError.TimedOut
                        Socket_Compelete_Request(Socket_Error_Type.Host_Connect_Timeout, Nothing, Nothing)
                        Exit Sub
                    Case Else
                        Socket_Compelete_Request(Socket_Error_Type.Host_Connect_Others, Nothing, Nothing)
                        Exit Sub
                End Select

                Select Case Http_Request_Protocol
                    Case Http_Request_Protocol_Type.Http
                        With Socket_Send_EventArgs
                            .AcceptSocket = Win_Socket
                            If Http_Request_Data.Length <= Http_Send_Buff_Size Then
                                .SetBuffer(Http_Request_Data, 0, Http_Request_Data.Length)
                                Http_Request_Current_Position = Http_Request_Data.Length
                                Http_Send_Count_Total += Http_Request_Data.Length
                            Else
                                'Debug.Print(System.Text.Encoding.UTF8.GetChars(Http_Request_Data))
                                .SetBuffer(Http_Request_Data, 0, Http_Send_Buff_Size)
                                Http_Request_Current_Position = Http_Send_Buff_Size
                                Http_Send_Count_Total += Http_Send_Buff_Size
                            End If
                        End With
                        Threading.ThreadPool.QueueUserWorkItem(AddressOf Moniter_Data_Send_Timeout)
                        If Win_Socket.SendAsync(Socket_Send_EventArgs) = False Then
                            Socket_Compelete_Request(Socket_Error_Type.Faild_Start_Send, Nothing, Nothing)

                        End If
                    Case Http_Request_Protocol_Type.Https
                        If Https_Stream_Client Is Nothing Then
                            Dim Ssl_Stream_Authenticate_Async As Threading.Tasks.Task
                            Https_Stream_Client = New NetworkStream(Win_Socket, System.IO.FileAccess.ReadWrite, True)
                            Https_Ssl_Stream_Client = New SslStream(Https_Stream_Client, False, New RemoteCertificateValidationCallback(AddressOf ValidateServerCertificate), Nothing)
                            Ssl_Stream_Authenticate_Async = Https_Ssl_Stream_Client.AuthenticateAsClientAsync("127.0.0.1")
                            Ssl_Stream_Authenticate_Async.Wait(Setting_Timeout_Ssl_Stream_Authenticate_Async)
                            If Ssl_Stream_Authenticate_Async.IsCompleted = False Then
                                Socket_Compelete_Request(Socket_Error_Type.Authenticate_Timeout, Nothing, Nothing)
                                Exit Sub
                            End If
                        End If
                        Threading.ThreadPool.QueueUserWorkItem(AddressOf Moniter_Host_Connect_Timeout, Socket_Connect_EventArgs)
                        Dim Ssl_Stream_Write_Async As Threading.Tasks.Task
                        With Https_Ssl_Stream_Client
                            If Http_Request_Data.Length <= Http_Send_Buff_Size Then
                                Ssl_Stream_Write_Async = .WriteAsync(Http_Request_Data, 0, Http_Request_Data.Length)
                                Http_Request_Current_Position = Http_Request_Data.Length
                                Http_Send_Count_Total += Http_Request_Data.Length
                            Else
                                Ssl_Stream_Write_Async = .WriteAsync(Http_Request_Data, 0, Http_Send_Buff_Size)
                                Http_Request_Current_Position = Http_Send_Buff_Size
                                Http_Send_Count_Total += Http_Send_Buff_Size
                            End If
                        End With
                        Ssl_Stream_Write_Async.Wait(Setting_Timeout_Ssl_Stream_Write_Async)
                        If Ssl_Stream_Write_Async.IsCompleted Then
                            Ssl_Stream_Write_Async = Https_Ssl_Stream_Client.FlushAsync()
                            Ssl_Stream_Write_Async.Wait(Setting_Timeout_Ssl_Stream_Write_Async)
                            Console.WriteLine("Https-Write-Initial")
                            If Ssl_Stream_Write_Async.IsCompleted Then
                                Socket_Send_EventArgs_Completed(Me, Socket_Send_EventArgs)
                            Else
                                Socket_Compelete_Request(Socket_Error_Type.Ssl_Stream_Write_Timeout, Nothing, Nothing)
                                Exit Sub
                            End If
                        Else
                            Socket_Compelete_Request(Socket_Error_Type.Ssl_Stream_Write_Timeout, Nothing, Nothing)
                            Exit Sub
                        End If
                End Select
            Catch ex As Exception
                Select Case ex.HResult
                    Case -2146232800
                        Socket_Compelete_Request(Socket_Error_Type.Disconnect_By_Host, Nothing, Nothing)
                    Case -2146233088
                        Socket_Compelete_Request(Socket_Error_Type.DataStructure_Error_Handshake_Failed, Nothing, Nothing)
                    Case Else
                        Socket_Compelete_Request(Socket_Error_Type.Other_Error, Nothing, Nothing)
                End Select
            End Try
        End Sub
        Private Sub Socket_Send_EventArgs_Completed(ByVal sender As Object, ByVal e As SocketAsyncEventArgs)
            Try
                Select Case Http_Request_Protocol
                    Case Http_Request_Protocol_Type.Http
                        If Http_Request_Current_Position = Http_Request_Data.Length Then
                            Http_Send_Stop_Signal = 1
                            With Socket_Receive_EventArgs
                                .AcceptSocket = Win_Socket
                                ReDim Http_Receive_Buff(Http_Receive_Buff_Size - 1)
                                .SetBuffer(Http_Receive_Buff, 0, Http_Receive_Buff_Size)
                            End With
                            Threading.ThreadPool.QueueUserWorkItem(AddressOf Moniter_Data_Receive_Timeout)
                            If Win_Socket.ReceiveAsync(Socket_Receive_EventArgs) = False Then
                                Socket_Compelete_Request(Socket_Error_Type.Faild_Start_Receive, Nothing, Nothing)
                                Exit Sub
                            End If
                        Else
                            With Socket_Send_EventArgs
                                '====================================有待考证===============================================
                                If Http_Request_Data.Length - Http_Request_Current_Position <= Http_Send_Buff_Size Then
                                    .SetBuffer(Http_Request_Data, Http_Request_Current_Position, Http_Request_Data.Length - Http_Request_Current_Position)
                                    Http_Request_Current_Position = Http_Request_Data.Length
                                    Http_Send_Count_Total += Http_Request_Data.Length - Http_Request_Current_Position
                                Else
                                    .SetBuffer(Http_Request_Data, Http_Request_Current_Position, Http_Send_Buff_Size)
                                    Http_Request_Current_Position += Http_Send_Buff_Size
                                    Http_Send_Count_Total += Http_Send_Buff_Size
                                End If
                            End With
                            If Win_Socket.SendAsync(Socket_Send_EventArgs) = False Then
                                Socket_Compelete_Request(Socket_Error_Type.Faild_Continu_Send, Nothing, Nothing)
                                Exit Sub
                            End If
                        End If
                    Case Http_Request_Protocol_Type.Https
                        If Http_Request_Current_Position = Http_Request_Data.Length Then
                            Http_Send_Stop_Signal = 1
                            Dim Ssl_Stream_Read_Async As Threading.Tasks.Task(Of Integer)
                            ReDim Http_Receive_Buff(Http_Receive_Buff_Size - 1)
                            Ssl_Stream_Read_Async = Https_Ssl_Stream_Client.ReadAsync(Http_Receive_Buff, 0, Http_Receive_Buff_Size)
                            Threading.ThreadPool.QueueUserWorkItem(AddressOf Moniter_Data_Receive_Timeout)
                            Ssl_Stream_Read_Async.Wait(Setting_Timeout_Ssl_Stream_Read_Async)
                            If Ssl_Stream_Read_Async.IsCompleted Then
                                Https_Receive_Count_Once = Ssl_Stream_Read_Async.GetAwaiter.GetResult
                                Console.WriteLine("Https-Read-Initial")
                                If Https_Receive_Count_Once = 0 Then
                                    Socket_Compelete_Request(Socket_Error_Type.Disconnect_By_Host, Nothing, Nothing)
                                    Exit Sub
                                Else
                                    Socket_Receive_EventArgs_Completed(Me, Socket_Receive_EventArgs)
                                End If
                            Else
                                Socket_Compelete_Request(Socket_Error_Type.Ssl_Stream_Read_Timeout, Nothing, Nothing)
                                Exit Sub
                            End If
                        Else
                            Dim Ssl_Stream_Write_Async As Threading.Tasks.Task
                            With Https_Ssl_Stream_Client
                                If Http_Request_Data.Length - Http_Request_Current_Position <= Http_Send_Buff_Size Then
                                    Ssl_Stream_Write_Async = .WriteAsync(Http_Request_Data, Http_Request_Current_Position, Http_Request_Data.Length - Http_Request_Current_Position)
                                    Http_Request_Current_Position = Http_Request_Data.Length
                                    Http_Send_Count_Total += Http_Request_Data.Length - Http_Request_Current_Position
                                Else
                                    Ssl_Stream_Write_Async = .WriteAsync(Http_Request_Data, Http_Request_Current_Position, Http_Send_Buff_Size)
                                    Http_Request_Current_Position += Http_Send_Buff_Size
                                    Http_Send_Count_Total += Http_Send_Buff_Size
                                End If
                                Ssl_Stream_Write_Async.Wait(Setting_Timeout_Ssl_Stream_Write_Async)

                                If Ssl_Stream_Write_Async.IsCompleted Then
                                    Ssl_Stream_Write_Async = .FlushAsync()
                                    Ssl_Stream_Write_Async.Wait(Setting_Timeout_Ssl_Stream_Write_Async)
                                    Console.WriteLine("Https-Write-Repeat")
                                    If Ssl_Stream_Write_Async.IsCompleted Then
                                        Socket_Send_EventArgs_Completed(Me, Socket_Send_EventArgs)
                                    Else
                                        Socket_Compelete_Request(Socket_Error_Type.Ssl_Stream_Write_Timeout, Nothing, Nothing)
                                        Exit Sub
                                    End If
                                Else
                                    Socket_Compelete_Request(Socket_Error_Type.Ssl_Stream_Write_Timeout, Nothing, Nothing)
                                    Exit Sub
                                End If
                            End With
                        End If
                End Select
            Catch ex As Exception
                Select Case ex.HResult
                    Case -2146232800
                        '异常，连接被主机终止
                        Socket_Compelete_Request(Socket_Error_Type.Other_Error, Nothing, Nothing)
                        Exit Sub
                    Case -2146233067
                        '异常，另一个read挂起中
                        Socket_Compelete_Request(Socket_Error_Type.Other_Error, Nothing, Nothing)
                        Exit Sub
                    Case Else
                        Socket_Compelete_Request(Socket_Error_Type.Other_Error, Nothing, Nothing)
                        Exit Sub
                End Select
            End Try
        End Sub
        Private Sub Socket_Receive_EventArgs_Completed(ByVal sender As Object, ByVal e As SocketAsyncEventArgs)
            Try
Start_Process:
                Dim Receive_Count_Once As Integer
                Select Case Http_Request_Protocol
                    Case Http_Request_Protocol_Type.Http
                        Receive_Count_Once = e.BytesTransferred
                    Case Http_Request_Protocol_Type.Https
                        Receive_Count_Once = Https_Receive_Count_Once
                End Select
                If Http_Request_Close_Signal = 1 Then Exit Sub
                If Receive_Count_Once <> 0 Then
                    Http_Receive_Is_First_Packet = False
                    Http_Receive_Packet_Null_Consecutive_Count = 0
                    Http_Receive_Count_Total += Receive_Count_Once
                    ReDim Preserve Http_Receive_Buff(Receive_Count_Once - 1)
                Http_Receive_Data = Byte_Array_Assemble(Http_Receive_Data, Http_Receive_Buff)
                If Http_Header_Separator_Position = -1 Then
                        Http_Header_Separator_Position = Byte_Array_Find(Http_Receive_Data, Http_Header_Separator)
                    If Http_Header_Separator_Position <> -1 And Http_Header_Separator_Position <> -1 Then
                        Dim Separator_Position_Start As Integer = Byte_Array_Find(Http_Receive_Data, Http_Header_Content_Length_Separator)
                        If Http_Header_Separator_Position + 3 <> Http_Receive_Data.Length - 1 Then
                            Http_Content_Separate = Byte_Array_Split(Http_Receive_Data, Http_Header_Separator_Position + 4, Http_Receive_Data.Length - 1)
                            Http_Headers_Separate = Byte_Array_Split(Http_Receive_Data, 0, Http_Header_Separator_Position + 3)
                        Else
                            Http_Headers_Separate = Http_Receive_Data
                        End If
                        If Separator_Position_Start <> -1 Then
                            Dim Separator_Position_End As Integer = Byte_Array_Find(Http_Receive_Data, Http_Header_Vbcrlf, Separator_Position_Start + Http_Header_Content_Length_Separator.Length)
                            Dim Http_Header_Content_Length_Bytes() As Byte = Byte_Array_Split(Http_Receive_Data, Separator_Position_Start + Http_Header_Content_Length_Separator.Length + 1, Separator_Position_End - 1)
                            Http_Header_Content_Length = Val(System.Text.Encoding.UTF8.GetChars(Http_Header_Content_Length_Bytes))
                            Http_Receive_Count_Headers = Http_Header_Separator_Position + Http_Header_Separator.Length
                            If Http_Receive_Count_Total - Http_Headers_Separate.Length = Http_Header_Content_Length Then
                                Socket_Compelete_Request(Socket_Error_Type.Successd, Http_Headers_Separate, Http_Content_Separate)
                                Exit Sub
                            End If
                        Else
                            Dim Http_Header_Transfer_Encoding_Chunked_Position As Integer = Byte_Array_Find(Http_Receive_Data, Http_Header_Transfer_Encoding_Chunked_Separator)
                            If Http_Header_Transfer_Encoding_Chunked_Position = -1 Then Http_Header_Transfer_Encoding_Chunked_Position = Byte_Array_Find(Http_Receive_Data, Http_Header_Transfer_Encoding_Chunked_Separator2)
                            If Http_Header_Transfer_Encoding_Chunked_Position <> -1 Then
                                Http_Header_Content_Length = -2
                                If Not Http_Content_Separate Is Nothing Then
                                    Http_Receive_Buff = Http_Content_Separate
                                    GoTo Begin_Process
                                End If
                            Else
                                Http_Header_Content_Length = -1
                                Socket_Compelete_Request(Socket_Error_Type.Successd, Http_Headers_Separate, Nothing)
                                Exit Sub
                            End If
                            Http_Content_Chunked_Total = Http_Content_Separate
                        End If
                    End If
                Else
Begin_Process:
                    If Http_Header_Content_Length >= 0 Then
                        Http_Content_Separate = Byte_Array_Assemble(Http_Content_Separate, Http_Receive_Buff)
                        If Http_Receive_Count_Total - Http_Receive_Count_Headers = Http_Header_Content_Length Then
                            Socket_Compelete_Request(Socket_Error_Type.Successd, Http_Headers_Separate, Http_Content_Separate)
                            Exit Sub
                        End If
                    ElseIf Http_Header_Content_Length = -2 Then
                        Http_Content_Chunked_Total = Byte_Array_Assemble(Http_Content_Chunked_Total, Http_Receive_Buff)
Pre_Process:
                            If Http_Content_Chunked_Single_Size > 0 Then
                                If Http_Content_Chunked_Total.Length - Http_Content_Chunked_Indicator + 1 - 1 >= Http_Content_Chunked_Single_Size Then
                                    Http_Content_Chunked_Indicator += (Http_Content_Chunked_Single_Size + 2)
                                    Http_Content_Chunked_Indicator_Records.Add(Http_Content_Chunked_Indicator - 3)
                                    Http_Content_Chunked_Single_Size = 0
                                    GoTo Further_Process
                                End If
                            Else
Further_Process:
                                Dim Http_Content_Chunked_Start_Position As Integer
                                Http_Content_Chunked_Start_Position = Byte_Array_Find(Http_Content_Chunked_Total, Http_Header_Vbcrlf, Http_Content_Chunked_Indicator)
                                If Http_Content_Chunked_Start_Position <> -1 Then
                                    Dim Chunked_Buff_Size() As Byte = Byte_Array_Split(Http_Content_Chunked_Total, Http_Content_Chunked_Indicator, Http_Content_Chunked_Start_Position - 1)
                                    Dim Chunked_Buff_Size_String = Byte_Array_To_Asc_Chars(Chunked_Buff_Size)
                                    Http_Content_Chunked_Single_Size = CInt("&H" & Chunked_Buff_Size_String)
                                    If Http_Content_Chunked_Single_Size <> 0 Then
                                        Http_Content_Chunked_Indicator = Http_Content_Chunked_Start_Position + 2
                                        Http_Content_Chunked_Indicator_Records.Add(Http_Content_Chunked_Indicator)
                                        GoTo Pre_Process
                                    Else
                                        If Http_Content_Chunked_Indicator_Records.Count Mod 2 = 0 Then
                                            Dim Tmp_Data_Assemble() As Byte = Nothing
                                            For i = 0 To Http_Content_Chunked_Indicator_Records.Count - 1 Step 2
                                                Tmp_Data_Assemble = Byte_Array_Assemble(Tmp_Data_Assemble, Byte_Array_Split(Http_Content_Chunked_Total, Http_Content_Chunked_Indicator_Records.Item(i), Http_Content_Chunked_Indicator_Records.Item(i + 1)))
                                            Next
                                            Socket_Compelete_Request(Socket_Error_Type.Successd, Http_Headers_Separate, Tmp_Data_Assemble)
                                            Exit Sub
                                        Else
                                            Socket_Compelete_Request(Socket_Error_Type.Received_Data_Corrupted, Nothing, Nothing)
                                            Exit Sub
                                        End If
                                    End If
                                End If
                            End If
                        End If
                    End If
                Else
                    If Http_Receive_Is_First_Packet = True Then
                        Socket_Compelete_Request(Socket_Error_Type.Faild_Receive_Data, Nothing, Nothing)
                        Exit Sub
                    Else
                        Http_Receive_Packet_Null_Consecutive_Count += 1
                        If Http_Receive_Packet_Null_Consecutive_Count >= 3 Then
                            If Http_Header_Content_Length >= 0 Then
                                Socket_Compelete_Request(Socket_Error_Type.Successd, Http_Headers_Separate, Http_Content_Separate)
                                Exit Sub
                            ElseIf Http_Header_Content_Length = -2 Then
                                Socket_Compelete_Request(Socket_Error_Type.Faild_Receive_Data, Nothing, Nothing)
                                Exit Sub
                            Else
                                Socket_Compelete_Request(Socket_Error_Type.Faild_Receive_Data, Nothing, Nothing)
                                Exit Sub
                            End If
                        End If
                    End If
                End If
                Select Case Http_Request_Protocol
                    Case Http_Request_Protocol_Type.Http
                        With Socket_Receive_EventArgs
                            ReDim Http_Receive_Buff(Http_Receive_Buff_Size - 1)
                            .SetBuffer(Http_Receive_Buff, 0, Http_Receive_Buff_Size)
                        End With
                        If Win_Socket.ReceiveAsync(Socket_Receive_EventArgs) = False Then
                            Socket_Compelete_Request(Socket_Error_Type.Faild_Continu_Receive, Nothing, Nothing)
                            Exit Sub
                        End If
                    Case Http_Request_Protocol_Type.Https
                        Dim Ssl_Stream_Read_Async As Threading.Tasks.Task(Of Integer)
                        ReDim Http_Receive_Buff(Http_Receive_Buff_Size - 1)
                        Ssl_Stream_Read_Async = Https_Ssl_Stream_Client.ReadAsync(Http_Receive_Buff, 0, Http_Receive_Buff_Size)
                        Ssl_Stream_Read_Async.Wait(Setting_Timeout_Ssl_Stream_Read_Async)
                        Console.WriteLine("Https-Read-Repeat")
                        If Ssl_Stream_Read_Async.IsCompleted Then
                            Https_Receive_Count_Once = Ssl_Stream_Read_Async.GetAwaiter.GetResult
                            GoTo Start_Process
                        Else
                            Socket_Compelete_Request(Socket_Error_Type.Ssl_Stream_Read_Timeout, Nothing, Nothing)
                            Exit Sub
                        End If
                End Select
            Catch ex As Exception
                Select Case ex.HResult
                    Case Else
                        Socket_Compelete_Request(Socket_Error_Type.Other_Error, Nothing, Nothing)
                        Exit Sub
                End Select
            End Try

        End Sub
        Public Sub Http_Request(Http_Request_Url As String, Http_Request_Mode As String, Http_Request_Headers As ArrayList, Http_Request_Data_Private() As Byte, ByRef Http_Receive_Data() As Byte, ByRef Manager_Http_Socket As Manager_Http_Socket, ByVal Http_Protocol_Type As String, ByVal Socket_Is_Close As Boolean, ByVal Http_Porxy_Setting_Address As String, ByVal Http_Porxy_Setting_Port As String, ByVal Http_Porxy_Is_First As Boolean)
            'Socket_Connect_EventArgs = New SocketAsyncEventArgs
            'Socket_Send_EventArgs = New SocketAsyncEventArgs
            'Socket_Receive_EventArgs = New SocketAsyncEventArgs
            If Timer.Stop_Time > Socket_Connect_Keep_Timeout Then Socket_Close()
            Timer.Start()
            AddHandler Socket_Connect_EventArgs.Completed, AddressOf Socket_Connect_EventArgs_Completed
            AddHandler Socket_Send_EventArgs.Completed, AddressOf Socket_Send_EventArgs_Completed
            AddHandler Socket_Receive_EventArgs.Completed, AddressOf Socket_Receive_EventArgs_Completed
            'Win_Socket = New Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            Win_Socket.SendBufferSize = Http_Send_Buff_Size
            Win_Socket.ReceiveBufferSize = Http_Receive_Buff_Size
            Http_Content_Chunked_Indicator_Records = New ArrayList
            Dim Request_Url_Head As String = Pickup_Data(Http_Request_Url, "", "://", True)
            Setting_Win_Socket_Is_Close = Socket_Is_Close
            Dim Request_Url_Content As String = Pickup_Data(Http_Request_Url, "://", "")
            Dim Request_Url_Domain As String
            Dim Http_Request_Url_Parameters As String
            Request_Url_Domain = Pickup_Data(Request_Url_Content, "", "/", True)
            Dim Request_Url_Port As Integer
            If InStr(Request_Url_Domain, ":") > 0 Then
                Request_Url_Port = Val(Pickup_Data(Request_Url_Domain, ":", ""))
            End If
            If InStr(Request_Url_Domain, ":") > 0 Then Request_Url_Domain = Pickup_Data(Request_Url_Domain, "", ":")
            Http_Request_Url_Parameters = Pickup_Data(Request_Url_Content, Request_Url_Domain, "")
            Select Case Request_Url_Head.ToUpper
                Case "HTTP"
                    Http_Request_Protocol = Http_Request_Protocol_Type.Http
                    If Request_Url_Port = 0 Then Request_Url_Port = 80
                Case "HTTPS"
                    Http_Request_Protocol = Http_Request_Protocol_Type.Https
                    If Request_Url_Port = 0 Then Request_Url_Port = 443
                Case Else
                    Socket_Compelete_Request(Socket_Error_Type.Unknown_Protocol, Nothing, Nothing)
                    Exit Sub
            End Select
            If Http_Porxy_Setting_Address = "" Then
                Http_Proxy_Enable = False
            ElseIf Http_Porxy_Is_First = True And Http_Porxy_Setting_Address <> "" Then
                Http_Proxy_Enable = True
                Http_Request_Protocol = Http_Request_Protocol_Type.Http
                Http_Proxy_Address = Http_Porxy_Setting_Address
                Http_Proxy_Port = Http_Porxy_Setting_Port
                'Request_Url_Domain = Request_Url_Domain 
                Http_Request_Url_Parameters = Request_Url_Domain & ":" & Request_Url_Port
            ElseIf Http_Porxy_Is_First = False And Http_Porxy_Setting_Address <> "" Then
                Http_Proxy_Enable = True
                'Http_Request_Protocol = Http_Request_Protocol_Type.Http
                Http_Proxy_Address = Http_Porxy_Setting_Address
                Http_Proxy_Port = Http_Porxy_Setting_Port
            End If
            Dim Request_Headers_String As String, Request_Headers_Buff() As Byte
            Request_Headers_String = Http_Request_Mode.ToUpper + " " + Http_Request_Url_Parameters + " " + Http_Protocol_Type
            Manager_Http_Socket_Ref = Manager_Http_Socket
            Request_Headers_String += vbCrLf
            If Not Http_Request_Headers Is Nothing Then
                For Each Request_Header In Http_Request_Headers
                    Request_Headers_String += Request_Header
                Next
            End If
            Request_Headers_String += vbCrLf
            Request_Headers_Buff = System.Text.Encoding.UTF8.GetBytes(Request_Headers_String)
            Http_Request_Data = Byte_Array_Assemble(Request_Headers_Buff, Http_Request_Data_Private)
        Debug.Print(System.Text.Encoding.UTF8.GetChars(Http_Request_Data))
        Http_Header_Content_Length = -1
            Http_Request_Close_Signal = 0
            Http_Request_Current_Position = 0
            Http_Header_Separator_Position = -1
            If Http_Porxy_Setting_Address = "" Then
                If Ip_Address_Vertify(Request_Url_Domain) Then
                    Host_Connect(Request_Url_Domain, Request_Url_Port)
                Else
                    Dim Host_Memery As String = Manager_Http_Socket_Ref.Ip_Get_From_Ip_Collection(Request_Url_Domain)
                    If Host_Memery <> "" Then
                        Host_Connect(Host_Memery, Request_Url_Port)
                    Else
                        Host_Get_From_Domain(Request_Url_Domain, Request_Url_Port)
                    End If
                End If
            Else
                Host_Connect("", 0)
            End If

        End Sub
        Private Sub Throw_Socket_Error(Socket_Error As Socket_Error_Type)
            Socket_Last_Error = Socket_Error
        End Sub
        Private Function ValidateServerCertificate(sender As Object, certificate As X509Certificate, chain As X509Chain, sslPolicyErrors As SslPolicyErrors) As Boolean
            Return True
            'if (sslPolicyErrors == SslPolicyErrors.None)
            '    return true;

            'Console.WriteLine("Certificate error: {0}", sslPolicyErrors);
            'return false;
        End Function
        Private Function Byte_Array_To_Asc_Chars(Byte_Asc() As Byte) As String
            Dim Content As String = ""
            For i = 0 To Byte_Asc.Count - 1
                Content += Chr(Byte_Asc(i))
            Next
            Return Content
        End Function
        Public Function Pickup_Data(Content As String, Prefix As String, Suffix As String, Optional NearMode As Boolean = False, Optional Start_Index As Integer = 1) As String
            Dim Pre_Index As Integer, Suf_Index As Integer
            If Prefix = "" Then
                Pre_Index = Start_Index
            Else
                If InStr(Start_Index, Content, Prefix) > 0 Then
                    Pre_Index = InStr(Start_Index, Content, Prefix) + 1 + Prefix.Length - 1
                Else
                    Return ""
                End If
            End If
            If Suffix = "" Then
                Suf_Index = Content.Length + 1
            Else
                If InStr(Pre_Index, Content, Suffix) > 0 Then
                    If NearMode Then
                        Suf_Index = InStr(Pre_Index, Content, Suffix)
                    Else
                        Suf_Index = InStrRev(Content, Suffix)
                    End If
                Else
                    Return ""
                End If
            End If
            If Suf_Index > Pre_Index Then
                Return Mid(Content, Pre_Index, Suf_Index - Pre_Index)
            Else
                Return ""
            End If
        End Function
        Public Function Ip_Address_Vertify(Content As String) As Boolean
            Dim rx As New Regex("((?:(?:25[0-5]|2[0-4]\d|((1\d{2})|([1-9]?\d)))\.){3}(?:25[0-5]|2[0-4]\d|((1\d{2})|([1-9]?\d))))")
            If rx.IsMatch(Content) Then
                Return True
            Else
                Return False
            End If
        End Function
        Public Function Byte_Array_Find(ByVal Scoure() As Byte, ByVal Content() As Byte, Optional Start_Index As Integer = 0, Optional ByVal Is_Reverse As Boolean = False) As Long
            Try
                Dim Arr_Match As Boolean = False
                If Is_Reverse = False Then
                    For i = Start_Index To Scoure.Length - 1
                        If Scoure(i) = Content(0) Then
                            Arr_Match = True
                            For w = 0 To Content.Length - 1
                                If Scoure(i + w) <> Content(w) Then
                                    Arr_Match = False
                                    Exit For
                                End If
                            Next
                            If Arr_Match = True Then
                                Byte_Array_Find = i
                                Exit Function
                            End If
                        End If
                    Next
                Else
                    For i = Scoure.Length - Content.Length To Start_Index Step -1
                        If Scoure(i) = Content(0) Then
                            Arr_Match = True
                            For w = 0 To Content.Length - 1
                                If Scoure(i + w) <> Content(w) Then
                                    Arr_Match = False
                                    Exit For
                                End If
                            Next
                            If Arr_Match = True Then
                                Byte_Array_Find = i
                                Exit Function
                            End If
                        End If
                    Next
                End If
                Return -1
            Catch ex As Exception
                Return -1
            End Try
        End Function
        Public Shared Function Byte_Array_Assemble(Source_A() As Byte, Source_B() As Byte) As Byte()
            If Source_A Is Nothing Then Return Source_B
            If Source_B Is Nothing Then Return Source_A
            Dim Source_AB(Source_A.Length + Source_B.Length - 1) As Byte
            Source_A.CopyTo(Source_AB, 0)
            Source_B.CopyTo(Source_AB, Source_A.Length)
            Return Source_AB
        End Function
        Public Function Byte_Array_Split(Source() As Byte, StartIndex As Integer, EndIndex As Integer) As Byte()
            Try
                Dim Result(EndIndex - StartIndex) As Byte
                For i As Integer = 0 To EndIndex - StartIndex
                    Result(i) = Source(i + StartIndex)
                Next
                Return Result
            Catch ex As IndexOutOfRangeException
                Throw New Exception(ex.Message)
            End Try
        End Function
    Private Sub Delay(ByVal DelayTime As Single)
        Dim Timer_Count As Single
        Dim Timer_Gap As Integer = 10
        Timer_Count = Timer_Gap + DelayTime
        While Timer_Count - Timer_Gap > 0
            Application.DoEvents()
            Timer_Count = Timer_Count - Timer_Gap
            System.Threading.Thread.Sleep(10)
        End While
    End Sub
End Class