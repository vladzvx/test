alter table public.phones add column timestamp timestamp default CURRENT_TIMESTAMP;
alter table public.phones drop constraint phones_pkey;
alter table public.phones add primary key (timestamp,user_id);