﻿CREATE TABLE Singers (
  SingerId  INT64 NOT NULL,
  FirstName STRING(200),
  LastName  STRING(200) NOT NULL,
  FullName  STRING(400) NOT NULL AS (COALESCE(FirstName || ' ', '') || LastName) STORED,
  BirthDate DATE,
  Picture   BYTES(MAX),
) PRIMARY KEY (SingerId);

CREATE INDEX Idx_Singers_FullName ON Singers (FullName);

CREATE TABLE Albums (
  AlbumId     INT64 NOT NULL,
  Title       STRING(100) NOT NULL,
  ReleaseDate DATE,
  Singer      INT64 NOT NULL,
  CONSTRAINT FK_Albums_Singers FOREIGN KEY (Singer) REFERENCES Singers (SingerId),
) PRIMARY KEY (AlbumId);

CREATE TABLE Tracks (
  AlbumId         INT64 NOT NULL,
  TrackId         INT64 NOT NULL,
  Title           STRING(200) NOT NULL,
  Duration        NUMERIC,
  LyricsLanguages ARRAY<STRING(2)>,
  Lyrics          ARRAY<STRING(MAX)>,
  CONSTRAINT Chk_Languages_Lyrics_Length_Equal CHECK (ARRAY_LENGTH(LyricsLanguages) = ARRAY_LENGTH(Lyrics)),
) PRIMARY KEY (AlbumId, TrackId), INTERLEAVE IN PARENT Albums;

CREATE UNIQUE INDEX Idx_Tracks_AlbumId_Title ON Tracks (TrackId, Title);

CREATE TABLE Venues (
  Code      STRING(10) NOT NULL,
  Name      STRING(100),
  Active    BOOL NOT NULL,
  Capacity  INT64,
  Ratings   ARRAY<FLOAT64>,
) PRIMARY KEY (Code);

CREATE TABLE Concerts (
  Venue     STRING(10) NOT NULL,
  StartTime TIMESTAMP NOT NULL,
  SingerId  INT64 NOT NULL,
  Title     STRING(200),
  CONSTRAINT FK_Concerts_Venues FOREIGN KEY (Venue) REFERENCES Venues (Code),
  CONSTRAINT FK_Concerts_Singers FOREIGN KEY (SingerId) REFERENCES Singers (SingerId),
) PRIMARY KEY (Venue, StartTime, SingerId);

CREATE TABLE Performances (
  Venue            STRING(10) NOT NULL,
  ConcertStartTime TIMESTAMP NOT NULL,
  SingerId         INT64 NOT NULL,
  AlbumId          INT64 NOT NULL,
  TrackId          INT64 NOT NULL,
  StartTime        TIMESTAMP,
  Rating           FLOAT64,
  CONSTRAINT FK_Performances_Concerts FOREIGN KEY (Venue, ConcertStartTime, SingerId) REFERENCES Concerts (Venue, StartTime, SingerId),
  CONSTRAINT FK_Performances_Singers FOREIGN KEY (SingerId) REFERENCES Singers (SingerId),
  CONSTRAINT FK_Performances_Tracks FOREIGN KEY (AlbumId, TrackId) REFERENCES Tracks (AlbumId, TrackId),
) PRIMARY KEY (Venue, SingerId, StartTime);

CREATE TABLE TableWithAllColumnTypes (
	ColInt64 INT64 NOT NULL,
	ColFloat64 FLOAT64 NOT NULL,
	ColBool BOOL NOT NULL,
	ColString STRING(100) NOT NULL,
	ColStringMax STRING(MAX) NOT NULL,
	ColBytes BYTES(100) NOT NULL,
	ColBytesMax BYTES(MAX) NOT NULL,
	ColDate DATE NOT NULL,
	ColTimestamp TIMESTAMP NOT NULL,
	ColCommitTS TIMESTAMP NOT NULL,
	ColInt64Array ARRAY<INT64>,
	ColFloat64Array ARRAY<FLOAT64>,
	ColBoolArray ARRAY<BOOL>,
	ColStringArray ARRAY<STRING(100)>,
	ColStringMaxArray ARRAY<STRING(MAX)>,
	ColBytesArray ARRAY<BYTES(100)>,
	ColBytesMaxArray ARRAY<BYTES(MAX)>,
	ColDateArray ARRAY<DATE>,
	ColTimestampArray ARRAY<TIMESTAMP>,
	ColComputed STRING(MAX) AS (ARRAY_TO_STRING(ColStringArray, ',')) STORED,
) PRIMARY KEY (ColInt64);
