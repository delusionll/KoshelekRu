CREATE TABLE IF NOT EXISTS messages
(
    id uuid NOT NULL,
    "time" timestamp without time zone NOT NULL,
    sernumber integer,
    content character varying(128) COLLATE pg_catalog."default" NOT NULL,
    CONSTRAINT messages_pkey PRIMARY KEY (id)
);