Imports TL
Imports ComponentFactory.Krypton.Toolkit
Imports System.Text
Imports System.Data
Imports System.Threading
Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports System.IO
Imports System.Net
Imports ComponentFactory.Krypton.Design
Imports ComponentFactory.Krypton.Docking
Imports ComponentFactory.Krypton.Navigator
Imports ComponentFactory.Krypton.Ribbon
Imports ComponentFactory.Krypton.Workspace

Namespace WindowsFormsApp3tttttest
    Partial Public Class Form1
        Inherits KryptonForm
        Private ReadOnly _codeReady As New ManualResetEventSlim()
        Private _client As WTelegram.Client
        Private _user As User

        Private Function Config(what As String, pphon As String) As String
            Select Case what
                Case "api_id"
                    Return txtAppId.Text
                Case "api_hash"
                    Return txtAppHash.Text
                Case "phone_number"
                    Return pphon
                Case "session_pathname"
                    Return "sessions/" & pphon & ".session"
                Case "verification_code", "password"
                    _codeReady.Reset()
                    _codeReady.Wait()
                    Return textBoxCode.Text
                Case Else
                    Return Nothing
            End Select
        End Function

        Private Sub CodeNeeded(what As String)
            labelCode.Text = what & ":"
            textBoxCode.Text = ""
            labelCode.Visible = textBoxCode.Visible = buttonSendCode.Visible = True
            textBoxCode.Focus()
            listBoxCmd.Items.Add($"A {what} is required...")
        End Sub

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub Form1_Load(sender As Object, e As EventArgs)
            cmbMinHour.SelectedIndex = 2
            cmbTimeInterval.SelectedIndex = 0
            cmbPref.SelectedIndex = 2

            txtAppId.Text = My.MySettings.[Default].api_id
            txtAppHash.Text = My.MySettings.[Default].api_hash
            txtPhone.Text = My.MySettings.[Default].phone_number

        End Sub

        Private Async Sub button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
            Try
                ProgressBar1.Visible = True
                listBoxCmd.Items.Clear()
                listBoxCmd.Items.Add($"Connecting & login {txtPhone.Text} into Telegram servers...")
                _client = New WTelegram.Client(Function(what) Config(what, txtPhone.Text))
                _user = Await _client.LoginUserIfNeeded()
                listBoxCmd.ForeColor = Color.LimeGreen
                listBoxCmd.Items.Add($"We are now connected as {_user}")

                ProgressBar1.Visible = False
                Button1.Enabled = False
                Button1.BackColor = Color.LimeGreen
                Button1.ForeColor = Color.White
                Button1.Text = "Connected"
            Catch ex As Exception
                MessageBox.Show(ex.Message)
                ProgressBar1.Visible = False
            End Try
        End Sub

        Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
            My.MySettings.[Default].api_id = txtAppId.Text
            My.MySettings.[Default].api_hash = txtAppHash.Text
            My.MySettings.[Default].phone_number = txtPhone.Text
            My.MySettings.[Default].Save()
        End Sub

        Private Async Sub button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
            Try
                ProgressBar1.Visible = True
                If _user Is Nothing Then
                    MessageBox.Show("You must login first.")
                    Return
                End If
                Dim chats = Await _client.Messages_GetAllChats(Nothing)
                listBoxCmd.Items.Clear()
                listBoxCmd.ForeColor = Color.Yellow
                For Each chat In chats.chats.Values
                    If chat.IsActive Then
                        listBoxCmd.Items.Add(chat)
                    End If
                Next
                ProgressBar1.Visible = False
            Catch ex As Exception
                MessageBox.Show(ex.Message)
                ProgressBar1.Visible = False
            End Try
        End Sub

        Private Sub buttonSendCode_Click(sender As Object, e As EventArgs) Handles buttonSendCode.Click
            labelCode.Visible = textBoxCode.Visible = buttonSendCode.Visible = False
            _codeReady.Set()
        End Sub

        Private Sub button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
            Try
                If DataGridView1.Rows.Count > 0 Then
                    Dim sfd As New SaveFileDialog()
                    sfd.Filter = "CSV (*.csv)|*.csv"
                    sfd.FileName = "Output.csv"
                    Dim fileError As Boolean = False
                    If sfd.ShowDialog() = DialogResult.OK Then
                        If File.Exists(sfd.FileName) Then
                            Try
                                File.Delete(sfd.FileName)
                            Catch ex As IOException
                                fileError = True
                                MessageBox.Show("It wasn't possible to write the data to the disk." & ex.Message)
                            End Try
                        End If
                        If Not fileError Then
                            Try
                                Dim columnCount As Integer = DataGridView1.Columns.Count
                                Dim columnNames As String = ""
                                Dim outputCsv(DataGridView1.Rows.Count) As String
                                For i As Integer = 0 To columnCount - 1
                                    columnNames += DataGridView1.Columns(i).HeaderText.ToString() & ","
                                Next
                                outputCsv(0) += columnNames

                                For i As Integer = 1 To DataGridView1.Rows.Count - 1
                                    For j As Integer = 0 To columnCount - 1
                                        outputCsv(i) += DataGridView1.Rows(i - 1).Cells(j).Value.ToString() & ","
                                    Next
                                Next

                                File.WriteAllLines(sfd.FileName, outputCsv, Encoding.UTF8)
                                MessageBox.Show("Members Exported Successfully !!!", "Info")
                            Catch ex As Exception
                                MessageBox.Show("Error :" & ex.Message)
                            End Try
                        End If
                    End If
                Else
                    MessageBox.Show("No Record To Export !!!", "Info")
                End If
            Catch ex As Exception
                MessageBox.Show(ex.Message)
            End Try
        End Sub

        Private Sub button5_Click(sender As Object, e As EventArgs) Handles Button5.Click
            ' Code for button5_Click event
        End Sub

        Private Async Sub button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
            ProgressBar1.Visible = True

            Try
                If TypeOf listBoxCmd.SelectedItem IsNot ChatBase Then
                    MessageBox.Show("You must select a chat in the list first")
                    ' kryptonButton4.Enabled = True;
                    ' kryptonButton3.Enabled = True;
                    ' buttonGetChatsKrypton.Enabled = True;
                    ' kryptonButton2.Enabled = True;
                    ' progressBar1.Visible = False;
                    Return
                End If
                Dim chat As ChatBase = DirectCast(listBoxCmd.SelectedItem, ChatBase)
                Dim users = If(TypeOf chat Is Channel, (Await _client!.Channels_GetAllParticipants(DirectCast(chat, Channel))).users, (Await _client.Messages_GetFullChat(chat.ID)).users)

                Dim table As New DataTable()

                table.Columns.Add("Username", GetType(String))
                table.Columns.Add("user id", GetType(String))
                table.Columns.Add("access hash", GetType(String))
                table.Columns.Add("group", GetType(String))
                table.Columns.Add("group id", GetType(String))

                Dim minHours As Integer = cmbMinHour.SelectedIndex
                Dim timeIntervals As Integer = cmbTimeInterval.SelectedIndex + 1
                Dim prefs As Integer = cmbPref.SelectedIndex

                Select Case prefs
                    Case 0
                        If minHours = 0 Then
                            For Each user In users.Values
                                If Not String.IsNullOrEmpty(user.username) AndAlso user.LastSeenAgo.TotalMinutes < timeIntervals Then
                                    table.Rows.Add(user.username, user.ID, user.access_hash, chat.Title, chat.ID)
                                End If
                            Next
                        ElseIf minHours = 1 Then
                            For Each user In users.Values
                                If Not String.IsNullOrEmpty(user.username) AndAlso user.LastSeenAgo.TotalHours < timeIntervals Then
                                    table.Rows.Add(user.username, user.ID, user.access_hash, chat.Title, chat.ID)
                                End If
                            Next
                        Else
                            For Each user In users.Values
                                If Not String.IsNullOrEmpty(user.username) Then
                                    table.Rows.Add(user.username, user.ID, user.access_hash, chat.Title, chat.ID)
                                End If
                            Next
                        End If
                    Case 1
                        If minHours = 0 Then
                            For Each user In users.Values
                                If String.IsNullOrEmpty(user.username) AndAlso user.LastSeenAgo.TotalMinutes < timeIntervals Then
                                    table.Rows.Add(user.username, user.ID, user.access_hash, chat.Title, chat.ID)
                                End If
                            Next
                        ElseIf minHours = 1 Then
                            For Each user In users.Values
                                If String.IsNullOrEmpty(user.username) AndAlso user.LastSeenAgo.TotalHours < timeIntervals Then
                                    table.Rows.Add(user.username, user.ID, user.access_hash, chat.Title, chat.ID)
                                End If
                            Next
                        Else
                            For Each user In users.Values
                                If String.IsNullOrEmpty(user.username) Then
                                    table.Rows.Add(user.username, user.ID, user.access_hash, chat.Title, chat.ID)
                                End If
                            Next
                        End If
                    Case 2
                        If minHours = 0 Then
                            For Each user In users.Values
                                If user.LastSeenAgo.TotalMinutes < timeIntervals Then
                                    table.Rows.Add(user.username, user.ID, user.access_hash, chat.Title, chat.ID)
                                End If
                            Next
                        ElseIf minHours = 1 Then
                            For Each user In users.Values
                                If user.LastSeenAgo.TotalHours < timeIntervals Then
                                    table.Rows.Add(user.username, user.ID, user.access_hash, chat.Title, chat.ID)
                                End If
                            Next
                        Else
                            For Each user In users.Values
                                table.Rows.Add(user.username, user.ID, user.access_hash, chat.Title, chat.ID)
                            Next
                        End If
                End Select

                DataGridView1.DataSource = table
                ProgressBar1.Visible = False
                label4.Text = DataGridView1.Rows.Count.ToString()
                label4.Visible = True
            Catch ex As Exception
                ProgressBar1.Visible = False
                MessageBox.Show(ex.Message)
            End Try
        End Sub

        Private Sub txtSearch_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtSearch.KeyPress
            DirectCast(DataGridView1.DataSource, DataTable).DefaultView.RowFilter = String.Format("Username LIKE '{0}%'", txtSearch.Text)
        End Sub

        Private Sub dataGridView1_RowHeaderMouseDoubleClick(sender As Object, e As DataGridViewCellMouseEventArgs)
            For Each row As DataGridViewRow In DataGridView1.SelectedRows
                DataGridView1.Rows.RemoveAt(row.Index)
                label4.Text = DataGridView1.Rows.Count.ToString()
            Next
        End Sub

        Friend WithEvents Button1 As Button
        Friend WithEvents Button2 As Button
        Friend WithEvents Button3 As Button
        Friend WithEvents Button4 As Button

        Private Sub InitializeComponent()
            Me.Button1 = New System.Windows.Forms.Button()
            Me.Button2 = New System.Windows.Forms.Button()
            Me.Button3 = New System.Windows.Forms.Button()
            Me.Button4 = New System.Windows.Forms.Button()
            Me.txtAppId = New System.Windows.Forms.TextBox()
            Me.txtAppHash = New System.Windows.Forms.TextBox()
            Me.textBoxCode = New System.Windows.Forms.TextBox()
            Me.labelCode = New System.Windows.Forms.Label()
            Me.buttonSendCode = New System.Windows.Forms.Button()
            Me.listBoxCmd = New System.Windows.Forms.ListBox()
            Me.txtPhone = New System.Windows.Forms.TextBox()
            Me.ProgressBar1 = New System.Windows.Forms.ProgressBar()
            Me.Button5 = New System.Windows.Forms.Button()
            Me.txtSearch = New System.Windows.Forms.TextBox()
            Me.label4 = New System.Windows.Forms.Label()
            Me.DataGridView1 = New System.Windows.Forms.DataGridView()
            Me.cmbPref = New System.Windows.Forms.ComboBox()
            Me.cmbTimeInterval = New System.Windows.Forms.ComboBox()
            Me.cmbMinHour = New System.Windows.Forms.ComboBox()
            Me.username = New System.Windows.Forms.DataGridViewTextBoxColumn()
            Me.IDD = New System.Windows.Forms.DataGridViewTextBoxColumn()
            Me.sssssssssssss = New System.Windows.Forms.DataGridViewTextBoxColumn()
            Me.ID = New System.Windows.Forms.TextBox()
            CType(Me.DataGridView1, System.ComponentModel.ISupportInitialize).BeginInit()
            Me.SuspendLayout()
            '
            'Button1
            '
            Me.Button1.Location = New System.Drawing.Point(147, 12)
            Me.Button1.Name = "Button1"
            Me.Button1.Size = New System.Drawing.Size(75, 23)
            Me.Button1.TabIndex = 0
            Me.Button1.Text = "Button1"
            Me.Button1.UseVisualStyleBackColor = True
            '
            'Button2
            '
            Me.Button2.Location = New System.Drawing.Point(156, 84)
            Me.Button2.Name = "Button2"
            Me.Button2.Size = New System.Drawing.Size(75, 23)
            Me.Button2.TabIndex = 1
            Me.Button2.Text = "Button2"
            Me.Button2.UseVisualStyleBackColor = True
            '
            'Button3
            '
            Me.Button3.Location = New System.Drawing.Point(147, 48)
            Me.Button3.Name = "Button3"
            Me.Button3.Size = New System.Drawing.Size(75, 23)
            Me.Button3.TabIndex = 2
            Me.Button3.Text = "Button3"
            Me.Button3.UseVisualStyleBackColor = True
            '
            'Button4
            '
            Me.Button4.Location = New System.Drawing.Point(156, 139)
            Me.Button4.Name = "Button4"
            Me.Button4.Size = New System.Drawing.Size(75, 23)
            Me.Button4.TabIndex = 3
            Me.Button4.Text = "Button4"
            Me.Button4.UseVisualStyleBackColor = True
            '
            'txtAppId
            '
            Me.txtAppId.Location = New System.Drawing.Point(12, 113)
            Me.txtAppId.Name = "txtAppId"
            Me.txtAppId.Size = New System.Drawing.Size(100, 20)
            Me.txtAppId.TabIndex = 4
            '
            'txtAppHash
            '
            Me.txtAppHash.Location = New System.Drawing.Point(22, 139)
            Me.txtAppHash.Name = "txtAppHash"
            Me.txtAppHash.Size = New System.Drawing.Size(100, 20)
            Me.txtAppHash.TabIndex = 5
            Me.txtAppHash.Text = "txtAppHash"
            '
            'textBoxCode
            '
            Me.textBoxCode.Location = New System.Drawing.Point(32, 165)
            Me.textBoxCode.Name = "textBoxCode"
            Me.textBoxCode.Size = New System.Drawing.Size(100, 20)
            Me.textBoxCode.TabIndex = 6
            Me.textBoxCode.Text = "TextBox1"
            '
            'labelCode
            '
            Me.labelCode.AutoSize = True
            Me.labelCode.Location = New System.Drawing.Point(163, 116)
            Me.labelCode.Name = "labelCode"
            Me.labelCode.Size = New System.Drawing.Size(39, 13)
            Me.labelCode.TabIndex = 7
            Me.labelCode.Text = "Label1"
            '
            'buttonSendCode
            '
            Me.buttonSendCode.Location = New System.Drawing.Point(147, 168)
            Me.buttonSendCode.Name = "buttonSendCode"
            Me.buttonSendCode.Size = New System.Drawing.Size(75, 23)
            Me.buttonSendCode.TabIndex = 8
            Me.buttonSendCode.Text = "buttonSendCode"
            Me.buttonSendCode.UseVisualStyleBackColor = True
            '
            'listBoxCmd
            '
            Me.listBoxCmd.FormattingEnabled = True
            Me.listBoxCmd.Items.AddRange(New Object() {"cmbMinHour", "ataTable"})
            Me.listBoxCmd.Location = New System.Drawing.Point(12, 12)
            Me.listBoxCmd.Name = "listBoxCmd"
            Me.listBoxCmd.Size = New System.Drawing.Size(120, 95)
            Me.listBoxCmd.TabIndex = 9
            '
            'txtPhone
            '
            Me.txtPhone.Location = New System.Drawing.Point(22, 191)
            Me.txtPhone.Name = "txtPhone"
            Me.txtPhone.Size = New System.Drawing.Size(100, 20)
            Me.txtPhone.TabIndex = 10
            Me.txtPhone.Text = "txtPhone"
            '
            'ProgressBar1
            '
            Me.ProgressBar1.Location = New System.Drawing.Point(59, 280)
            Me.ProgressBar1.Name = "ProgressBar1"
            Me.ProgressBar1.Size = New System.Drawing.Size(100, 23)
            Me.ProgressBar1.TabIndex = 12
            '
            'Button5
            '
            Me.Button5.Location = New System.Drawing.Point(147, 191)
            Me.Button5.Name = "Button5"
            Me.Button5.Size = New System.Drawing.Size(75, 23)
            Me.Button5.TabIndex = 13
            Me.Button5.Text = "Button5"
            Me.Button5.UseVisualStyleBackColor = True
            '
            'txtSearch
            '
            Me.txtSearch.Location = New System.Drawing.Point(122, 235)
            Me.txtSearch.Name = "txtSearch"
            Me.txtSearch.Size = New System.Drawing.Size(100, 20)
            Me.txtSearch.TabIndex = 14
            Me.txtSearch.Text = "txtSearch"
            '
            'label4
            '
            Me.label4.AutoSize = True
            Me.label4.Location = New System.Drawing.Point(322, 68)
            Me.label4.Name = "label4"
            Me.label4.Size = New System.Drawing.Size(35, 13)
            Me.label4.TabIndex = 15
            Me.label4.Text = "label4"
            '
            'DataGridView1
            '
            Me.DataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
            Me.DataGridView1.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.username, Me.IDD, Me.sssssssssssss})
            Me.DataGridView1.Location = New System.Drawing.Point(277, 153)
            Me.DataGridView1.Name = "DataGridView1"
            Me.DataGridView1.Size = New System.Drawing.Size(240, 150)
            Me.DataGridView1.TabIndex = 16
            '
            'cmbPref
            '
            Me.cmbPref.FormattingEnabled = True
            Me.cmbPref.Location = New System.Drawing.Point(405, 28)
            Me.cmbPref.Name = "cmbPref"
            Me.cmbPref.Size = New System.Drawing.Size(121, 21)
            Me.cmbPref.TabIndex = 17
            '
            'cmbTimeInterval
            '
            Me.cmbTimeInterval.FormattingEnabled = True
            Me.cmbTimeInterval.Location = New System.Drawing.Point(405, 60)
            Me.cmbTimeInterval.Name = "cmbTimeInterval"
            Me.cmbTimeInterval.Size = New System.Drawing.Size(121, 21)
            Me.cmbTimeInterval.TabIndex = 18
            '
            'cmbMinHour
            '
            Me.cmbMinHour.FormattingEnabled = True
            Me.cmbMinHour.Location = New System.Drawing.Point(405, 87)
            Me.cmbMinHour.Name = "cmbMinHour"
            Me.cmbMinHour.Size = New System.Drawing.Size(121, 21)
            Me.cmbMinHour.TabIndex = 19
            '
            'username
            '
            Me.username.HeaderText = "username"
            Me.username.Name = "username"
            Me.username.ReadOnly = True
            '
            'IDD
            '
            Me.IDD.HeaderText = "User ID"
            Me.IDD.Name = "IDD"
            Me.IDD.ReadOnly = True
            '
            'sssssssssssss
            '
            Me.sssssssssssss.HeaderText = "ssss"
            Me.sssssssssssss.Name = "sssssssssssss"
            '
            'ID
            '
            Me.ID.Location = New System.Drawing.Point(381, 316)
            Me.ID.Name = "ID"
            Me.ID.Size = New System.Drawing.Size(100, 20)
            Me.ID.TabIndex = 20
            Me.ID.Text = "ID"
            '
            'Form1
            '
            Me.ClientSize = New System.Drawing.Size(572, 348)
            Me.Controls.Add(Me.ID)
            Me.Controls.Add(Me.cmbMinHour)
            Me.Controls.Add(Me.cmbTimeInterval)
            Me.Controls.Add(Me.cmbPref)
            Me.Controls.Add(Me.DataGridView1)
            Me.Controls.Add(Me.label4)
            Me.Controls.Add(Me.txtSearch)
            Me.Controls.Add(Me.Button5)
            Me.Controls.Add(Me.ProgressBar1)
            Me.Controls.Add(Me.txtPhone)
            Me.Controls.Add(Me.listBoxCmd)
            Me.Controls.Add(Me.buttonSendCode)
            Me.Controls.Add(Me.labelCode)
            Me.Controls.Add(Me.textBoxCode)
            Me.Controls.Add(Me.txtAppHash)
            Me.Controls.Add(Me.txtAppId)
            Me.Controls.Add(Me.Button4)
            Me.Controls.Add(Me.Button3)
            Me.Controls.Add(Me.Button2)
            Me.Controls.Add(Me.Button1)
            Me.Name = "Form1"
            CType(Me.DataGridView1, System.ComponentModel.ISupportInitialize).EndInit()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub

        Friend WithEvents txtAppId As TextBox
        Friend WithEvents txtAppHash As TextBox
        Friend WithEvents textBoxCode As TextBox
        Friend WithEvents labelCode As Label
        Friend WithEvents buttonSendCode As Button
        Friend WithEvents listBoxCmd As ListBox
        Friend WithEvents txtPhone As TextBox
        Friend WithEvents ProgressBar1 As ProgressBar
        Friend WithEvents Button5 As Button
        Friend WithEvents txtSearch As TextBox
        Friend WithEvents label4 As Label
        Friend WithEvents DataGridView1 As DataGridView
        Friend WithEvents cmbPref As ComboBox
        Friend WithEvents cmbTimeInterval As ComboBox
        Friend WithEvents cmbMinHour As ComboBox
        Friend WithEvents username As DataGridViewTextBoxColumn
        Friend WithEvents IDD As DataGridViewTextBoxColumn
        Friend WithEvents sssssssssssss As DataGridViewTextBoxColumn
        Friend WithEvents ID As TextBox
    End Class
End Namespace