Imports System.IO
Imports System.ComponentModel

Public Class Form1
    Dim Version As String = "V1.5" 'Version der Anwendung
    Dim Zielpfad As String = ""
    Dim Quelle As String = ""
    Dim Videos As FileInfo()
    Dim Fotos As FileInfo()
    Dim FileSizeSumm As Double 'Summe der Videogrößen in GigaByte
    Dim Kilo As Long = 1024 'Divisor um Bytes als KB oder MB oder GB anzuzeigen
    Dim copied As Boolean = False
    Private copyBGW As BackgroundWorker

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        'Quellordner wählen
        FB1.RootFolder = Environment.SpecialFolder.MyComputer
        If FB1.ShowDialog() = DialogResult.OK Then
            TextBox1.Text = FB1.SelectedPath
            Quelle = FB1.SelectedPath
            collectFiles()

            watchSRCfolder.Path = Quelle
            watchSRCfolder.EnableRaisingEvents = True
            Show_Files()
            CalcFileSize()
            copied = False
        End If
    End Sub

    Sub collectFiles()
        Dim di As New DirectoryInfo(Quelle)
        'Lade alle Videodateien in Array "Videos"
        Videos = di.GetFiles("*.MP4")
        Fotos = di.GetFiles("*.JPG")
    End Sub

    Sub CalcFileSize()
        'errechnet die Summe der Videogrößen und schreibt Ergebins in Label9 in GB
        FileSizeSumm = 0
        For Each Video In Videos
            FileSizeSumm += (Video.Length / Kilo / Kilo / Kilo)
        Next
        FileSizeSumm = Math.Round(FileSizeSumm, 2)
        Label9.Text = "Summe Videogröße: " & FileSizeSumm & " GB"
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        'Zielordner wählen
        FB2.RootFolder = Environment.SpecialFolder.MyComputer
        If FB2.ShowDialog() = DialogResult.OK Then
            TextBox2.Text = FB2.SelectedPath
            Zielpfad = FB2.SelectedPath
            ShowFreeSpace()

            watchDSTfolder.Path = Zielpfad
            watchDSTfolder.EnableRaisingEvents = True
            Show_Files2()
            copied = False
        End If
    End Sub

    Sub ShowFreeSpace()
        'Ruft freien Speicher von Zielordner ab und schreibt Wert in Label10 in GB
        Path.GetPathRoot(Zielpfad)
        Dim Drive As DriveInfo = New DriveInfo(Path.GetPathRoot(Zielpfad))
        Label10.Text = "Verfügbarer Speicher: " & Math.Round(Drive.AvailableFreeSpace / Kilo / Kilo / Kilo, 2) & " GB"
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        'Videos kopieren
        Button1.Enabled = False
        Button2.Enabled = False
        Button3.Enabled = False
        Button4.Enabled = False

        Dim KameraNr As String = ComboBox1.SelectedItem
        If KameraNr IsNot Nothing And Videos IsNot Nothing And Not Zielpfad = "" Then
            If Not Videos.Length = 0 Or Not Fotos.Length = 0 Then

                copyBGW = New BackgroundWorker
                copyBGW.WorkerReportsProgress = True
                AddHandler copyBGW.DoWork, AddressOf copyFiles
                AddHandler copyBGW.RunWorkerCompleted, AddressOf copyFilesCompleted

                Label8.Visible = True
                If Not copyBGW.IsBusy Then
                    copyBGW.RunWorkerAsync({KameraNr})
                End If
            Else
                MsgBox("Keine Rohdaten im Quellordner!")
            End If
        Else
            If KameraNr Is Nothing Then
                MsgBox("Fehler, keine Kamera gewählt!")
                copied = False
            End If
            If Videos Is Nothing Then
                MsgBox("Fehler, keine Quelle gewählt!")
                copied = False
            End If
            If Zielpfad = "" Then
                MsgBox("Fehler, kein Zielordner gewählt!")
                copied = False
            End If
        End If
line3:
        ProgressBar1.Value = 0
    End Sub

    Private Sub copyFiles(ByVal sender As Object, ByVal e As System.ComponentModel.DoWorkEventArgs)
        Dim KameraNr As String = DirectCast(e.Argument(0), String)
        Dim vidcount As Integer = 0
        Dim piccount As Integer = 0
        Dim copiedBytes As Double = 0

        Try
            Dim sizeInBytes As Long
            Dim sizeInGigaBytes As Double
            For Each Einzelvideo In Videos
                sizeInBytes += Einzelvideo.Length
            Next
            For Each Einzelfoto In Fotos
                sizeInBytes += Einzelfoto.Length
            Next
            Me.Invoke(Sub() ProgressBar1.Maximum = sizeInBytes / Kilo / Kilo)

            Me.Invoke(Sub() ProgressBar1.Maximum = sizeInBytes / Kilo / Kilo)
            sizeInGigaBytes = sizeInBytes / Kilo / Kilo / Kilo
            sizeInGigaBytes = Math.Round(sizeInGigaBytes, 2)
            Me.Invoke(Sub() Label8.Text = sizeInGigaBytes & " GB")
            Me.Invoke(Sub() ProgressBar1.Value = ProgressBar1.Maximum / 100)

            For Each Einzelvideo In Videos
                If Einzelvideo.Extension = ".mp4" Or Einzelvideo.Extension = ".MP4" Then 'Filtert Dateien raus, die kein mp4-Video sind.
                    Dim Aufnahmedatum As String = Einzelvideo.CreationTime.ToString("yyMMdd_HHmm")
                    Dim Ausgabename As String = Zielpfad & "/" & Aufnahmedatum & "_" & KameraNr & ".mp4"

                    'Kopiervorgang mit Zähler bei bereits existierenden Dateinamen
                    Dim i As Integer = 0
line1:
                    If Not File.Exists(Ausgabename) Then
                        My.Computer.FileSystem.CopyFile(Einzelvideo.FullName, Ausgabename)
                        i = 0
                    ElseIf i >= 1800 Then
                        MsgBox("Datei " & Path.GetFileName(Ausgabename) & " existiert bereits!" & vbCrLf & "Bitte prüfen und manuell kopieren!")
                        vidcount -= 1
                    Else
                        i += 1
                        Ausgabename = Zielpfad & "/" & Aufnahmedatum & "_" & i & "_" & KameraNr & ".mp4"
                        GoTo line1
                    End If
                End If
                copiedBytes += Einzelvideo.Length
                Try
                    Me.Invoke(Sub() ProgressBar1.Value = copiedBytes / Kilo / Kilo)
                Catch
                    Me.Invoke(Sub() ProgressBar1.Value = ProgressBar1.Maximum)
                End Try

                Me.Invoke(Sub() Label8.Text = Math.Round(copiedBytes / Kilo / Kilo / Kilo, 2) & " GB von " & sizeInGigaBytes & " GB")
                vidcount += 1
            Next

            For Each EinzelFoto In Fotos
                If EinzelFoto.Extension = ".jpg" Or EinzelFoto.Extension = ".JPG" Then 'Filtert Dateien raus, die kein mp4-Video sind.
                    Dim Aufnahmedatum As String = EinzelFoto.CreationTime.ToString("yyMMdd_HHmm")
                    Dim Ausgabename As String = Zielpfad & "/" & "PIC_" & Aufnahmedatum & "_" & KameraNr & ".jpg"

                    'Kopiervorgang mit Zähler bei bereits existierenden Dateinamen
                    Dim i As Integer = 0
line2:
                    If Not File.Exists(Ausgabename) Then
                        My.Computer.FileSystem.CopyFile(EinzelFoto.FullName, Ausgabename)
                        i = 0
                    ElseIf i >= 1800 Then
                        MsgBox("Datei " & Path.GetFileName(Ausgabename) & " existiert bereits!" & vbCrLf & "Bitte prüfen und manuell kopieren!")
                        piccount -= 1
                    Else
                        i += 1
                        Ausgabename = Zielpfad & "/" & "PIC_" & Aufnahmedatum & "_" & i & "_" & KameraNr & ".jpg"
                        GoTo line2
                    End If
                End If
                piccount += 1
                copiedBytes += EinzelFoto.Length
                Try
                    Me.Invoke(Sub() ProgressBar1.Value = copiedBytes / Kilo / Kilo)
                Catch
                    Me.Invoke(Sub() ProgressBar1.Value = ProgressBar1.Maximum)
                End Try
            Next


        Catch
        End Try
    End Sub

    Private Sub copyFilesCompleted()
        copied = True
        Label8.Visible = False
        MsgBox("Kopiervorgang abgeschlossen")
        ShowFreeSpace()

        watchSRCfolder.EnableRaisingEvents = True
        watchDSTfolder.EnableRaisingEvents = True

        Button1.Enabled = True
        Button2.Enabled = True
        Button3.Enabled = True
        Button4.Enabled = True
        'Show_Files()
        'Show_Files2()
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        'Quellordner leeren
        If Not Quelle = "" Then
            Dim di As New DirectoryInfo(Quelle)
            Fotos = di.GetFiles("*.JPG")
            If copied = False Then
                MsgBox("Achtung! Die Quelldateien wurden noch nicht kopiert!")
            End If
            Dim result As Integer = MessageBox.Show("Wirklich alles aus Quelle löschen?", "Achtung!", MessageBoxButtons.OKCancel)
                If result = DialogResult.OK Then
                    Try
                        For Each Datei In di.GetFiles()
                            File.Delete(Datei.FullName)
                        Next
                    Catch ex As Exception
                        MsgBox(ex.Message)
                    End Try
                    MsgBox("Quelldateien gelöscht!")
                    Show_Files()
                    Show_Files2()
                Else
                    MsgBox("Nichts gelöscht!")
                End If
            Else
                MsgBox("Kein Quellverzeichnis ausgewählt!")
        End If
    End Sub

    Public watchSRCfolder As FileSystemWatcher
    Public watchDSTfolder As FileSystemWatcher

    Private Sub Show_Files()
        'Auflistung der Dateien im Quellordner
        If InvokeRequired Then
            Me.Invoke(Sub() ListBox1.Items.Clear())
        Else
            ListBox1.Items.Clear()
        End If
        If Not Quelle = "" Then
            Try
                Dim Files As String() = Directory.GetFiles(Quelle)
                If InvokeRequired Then
                    Me.Invoke(Sub() TextBox1.Text = Quelle)
                Else
                    TextBox1.Text = Quelle
                End If
                collectFiles()
                CalcFileSize()
                If Files.Length <= 0 Then
                    If InvokeRequired Then
                        Me.Invoke(Sub() ListBox1.Items.Add("- leer -"))
                    Else
                        ListBox1.Items.Add("- leer -")
                    End If
                Else
                    For Each File In Files
                        If InvokeRequired Then
                            Me.Invoke(Sub() ListBox1.Items.Add(Path.GetFileName(File)))
                        Else
                            ListBox1.Items.Add(Path.GetFileName(File))
                        End If
                    Next
                End If
            Catch
                'Quelle = ""
                If InvokeRequired Then
                    Me.Invoke(Sub() TextBox1.Text = "")
                Else
                    TextBox1.Text = ""
                End If
                copied = False
            End Try
        End If
    End Sub

    Private Sub Show_Files2()
        'Auflistung der Dateien im Zielordner
        If InvokeRequired Then
            Me.Invoke(Sub() ListBox2.Items.Clear())
        Else
            ListBox2.Items.Clear()
        End If

        If Not Zielpfad = "" Then
            Try
                Dim Files As String() = Directory.GetFiles(Zielpfad)
                If InvokeRequired Then
                    Me.Invoke(Sub() ShowFreeSpace())
                Else
                    ShowFreeSpace()
                End If

                If Files.Length <= 0 Then
                    If InvokeRequired Then
                        Me.Invoke(Sub() ListBox2.Items.Add("- leer -"))
                    Else
                        ListBox2.Items.Add("- leer -")
                    End If
                Else
                        For Each File In Files
                        If InvokeRequired Then
                            Me.Invoke(Sub() ListBox2.Items.Add(Path.GetFileName(File)))
                        Else
                            ListBox2.Items.Add(Path.GetFileName(File))
                        End If
                    Next
                End If
            Catch
                'Zielpfad = ""
                If InvokeRequired Then
                    Me.Invoke(Sub() TextBox2.Text = "")
                Else
                    TextBox2.Text = ""
                End If
                copied = False
            End Try
        End If
    End Sub

    Private Sub Button5_Click(sender As Object, e As EventArgs) Handles Button5.Click
        'Quellordner öffnen
        If Not Quelle = "" Then
            Process.Start(Quelle)
        End If
    End Sub

    Private Sub Button6_Click(sender As Object, e As EventArgs) Handles Button6.Click
        'Zielordner öffnen
        If Not Zielpfad = "" Then
            Process.Start(Zielpfad)
        End If
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        'Version des Programms im Titel einblenden
        Me.Text = Me.Text & " " & Version
        'Timer1.Enabled = True
        watchSRCfolder = New System.IO.FileSystemWatcher()
        watchDSTfolder = New System.IO.FileSystemWatcher()

        watchSRCfolder.NotifyFilter = IO.NotifyFilters.DirectoryName
        watchSRCfolder.NotifyFilter = watchSRCfolder.NotifyFilter Or
                                   IO.NotifyFilters.FileName
        watchSRCfolder.NotifyFilter = watchSRCfolder.NotifyFilter Or
                                   IO.NotifyFilters.Attributes

        watchDSTfolder.NotifyFilter = IO.NotifyFilters.DirectoryName
        watchDSTfolder.NotifyFilter = watchDSTfolder.NotifyFilter Or
                                   IO.NotifyFilters.FileName
        watchDSTfolder.NotifyFilter = watchDSTfolder.NotifyFilter Or
                                   IO.NotifyFilters.Attributes

        AddHandler watchSRCfolder.Changed, AddressOf Show_Files
        AddHandler watchSRCfolder.Created, AddressOf Show_Files
        AddHandler watchSRCfolder.Deleted, AddressOf Show_Files

        AddHandler watchDSTfolder.Changed, AddressOf Show_Files2
        AddHandler watchDSTfolder.Created, AddressOf Show_Files2
        AddHandler watchDSTfolder.Deleted, AddressOf Show_Files2

    End Sub

End Class
