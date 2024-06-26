﻿-- Database --

use master;
if DB_ID('Dodkin') is not null 
begin
    alter database Dodkin set single_user with rollback immediate;
    drop database Dodkin;
end;

if @@Error = 3702
   RaisError('Cannot delete the database because of the open connections.', 127, 127) with nowait, log;

create database Dodkin collate Latin1_General_100_CI_AS_SC;
go

use Dodkin;
go

-- Schemas --

create schema job authorization dbo;
go

-- Tables --

create table job.Delivery
(
    MessageId varchar(50) not null primary key,
    Message varbinary(8000) not null,
    MessageLabel nvarchar(249) null,
    Destination varchar(250) not null,
    DueTime datetimeoffset not null index IX_Delivery_DueTime,
    RetryCount smallint not null default 0
);