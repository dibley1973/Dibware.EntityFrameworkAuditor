CREATE TABLE [dbo].[AuditLog] (
    [Id]         INT              IDENTITY (1, 1) NOT NULL,
    [BatchId]    UNIQUEIDENTIFIER NULL,
    [ObjectType] NVARCHAR (MAX)   NOT NULL,
    [KeyMembers] NVARCHAR (MAX)   NOT NULL,
    [KeyValues]  NVARCHAR (MAX)   NULL,
    [Action]     NVARCHAR (50)    NOT NULL,
    [Property]   NVARCHAR (MAX)   NOT NULL,
    [OldValue]   NVARCHAR (MAX)   NULL,
    [NewValue]   NVARCHAR (MAX)   NULL,
    [Username]   NVARCHAR (100)   NOT NULL,
    [UtcDate]    SMALLDATETIME    CONSTRAINT [DF_DefaultLogDate] DEFAULT (GETDATE()) NOT NULL,
    CONSTRAINT [PK_AuditLog] PRIMARY KEY CLUSTERED ([Id] ASC)
);