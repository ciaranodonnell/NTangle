﻿{{! Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/NTangle }}
CREATE TABLE [{{OutboxSchema}}].[{{OutboxTable}}Data] (
  /*
   * This is automatically generated; any changes will be lost.
   */

  [{{OutboxTable}}DataId] BIGINT NOT NULL PRIMARY KEY CLUSTERED ([{{OutboxTable}}DataId] ASC) CONSTRAINT FK_{{OutboxSchema}}_{{OutboxTable}}Data_{{OutboxTable}} FOREIGN KEY REFERENCES [{{OutboxSchema}}].[{{OutboxTable}}] ([{{OutboxTable}}Id]) ON DELETE CASCADE,
  [EventId] NVARCHAR(128),
  [Type] NVARCHAR(1024) NULL,
  [Source] NVARCHAR(1024) NULL,
  [Timestamp] DATETIMEOFFSET,
  [CorrelationId] NVARCHAR(128),
  [EventData] VARBINARY(MAX) NULL
);