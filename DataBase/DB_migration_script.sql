alter table public.phones add column dbid bigserial;
alter table public.phones drop constraint phones_pkey;
alter table public.phones add primary key (dbid);