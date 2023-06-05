IF  NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TempDataLog]') AND type in (N'U')) 
BEGIN
    create table TempDataLog(
		id int primary key identity(1,1),
		[FileName] varchar(100) null, 
		PathLocation varchar(200) null, 
		[App_Name] varchar(100) null,
		Log_Code varchar(200) null, 
		Log_Message varchar(max) null, 
		Log_Type varchar(100) null, 
		Log_Date datetime null,
		StatusProcess int null,
		UploadTime datetime null,
		InProcessTime datetime null,
		FinishTime datetime null,
	)
END