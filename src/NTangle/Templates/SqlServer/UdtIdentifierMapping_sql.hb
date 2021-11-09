﻿{{! Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/NTangle }}
CREATE TYPE [{{CdcSchema}}].[udt{{IdentifierMappingTable}}List] AS TABLE (
  /*
   * This is automatically generated; any changes will be lost.
   */

  [Schema] VARCHAR(50) NOT NULL,
  [Table] VARCHAR(128) NOT NULL,
  [Key] NVARCHAR(128) NOT NULL,
  [GlobalId] {{IdentifierMappingSqlType}} NOT NULL
)