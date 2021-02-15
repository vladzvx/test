alter table tasks drop constraint tasks_source_message_id_fkey;
alter table tasks add column message_id_in_source_chat bigint;
CREATE OR REPLACE FUNCTION add_message_to_task() RETURNS trigger as
$$
    declare
        bot_uid bigint;
    begin
        bot_uid = (select bots.bot_user_id from bots where bots.bot_id=new.bot_id);
        if not bot_uid=new.user_id  then
            update public.tasks SET source_message_id=new.message_db_id, message_id_in_source_chat = new.message_id
                where bot_id=new.bot_id and source_chat = new.chat_id and task_status=1 and source_message_id is null;
        end if;
        return null;
    end;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION task_complited(source_chat_id bigint,_source_message_id bigint,target_chat_id bigint,bot_token text ) RETURNS void as
$$
    begin
        update tasks set task_status=3 where source_chat=source_chat_id and message_id_in_source_chat=_source_message_id and target_chat=target_chat_id and bot_id=get_bot_id(bot_token);
    end;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION task_not_complited(source_chat_id bigint,_source_message_id bigint,target_chat_id bigint,bot_token text ) RETURNS void as
$$
    begin
        update tasks set task_status=2 where source_chat=source_chat_id and tasks.message_id_in_source_chat=_source_message_id and target_chat=target_chat_id and bot_id=get_bot_id(bot_token);
    end;
$$ LANGUAGE plpgsql;

drop function get_active_tasks(text);
drop function get_future_tasks(text);

CREATE OR REPLACE FUNCTION get_active_tasks(bot_token text ) RETURNS TABLE (_source_chat bigint,
                                                                            _message_id bigint,
                                                                            _task_type text,
                                                                            _target_chat bigint,
                                                                            text varchar(4000),
                                                                            caption varchar(1000),
                                                                            media_id text,
                                                                            media_group_id text,
                                                                            task_id int,
                                                                            _action_time timestamp,
                                                                            chat_name text,
																			_message_id2 bigint) as
$$
    begin
        RETURN QUERY
            select distinct source_chat,message_id,task_type,target_chat,messages.text,messages.caption,messages.media_id,messages.media_group_id,tasks.task_id, tasks.action_time,c.name,tasks.message_id_in_source_chat from tasks inner join messages on tasks.source_message_id=messages.message_db_id inner join chats c on tasks.source_chat = c.chat_id
            where tasks.bot_id=get_bot_id(bot_token) and action_time<CURRENT_TIMESTAMP and task_status<3;
    end;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION get_future_tasks(bot_token text ) RETURNS TABLE (_source_chat bigint,
                                                                            _message_id bigint,
                                                                            _task_type text,
                                                                            _target_chat bigint,
                                                                            text varchar(4000),
                                                                            caption varchar(1000),
                                                                            media_id text,
                                                                            media_group_id text,
                                                                            task_id int,
                                                                            _action_time timestamp,
                                                                            chat_name text,
                                                                            _message_id2 bigint) as
$$
    begin
        RETURN QUERY
            select source_chat,message_id,task_type,target_chat,messages.text,messages.caption,messages.media_id,messages.media_group_id,tasks.task_id, tasks.action_time,c.name,tasks.message_id_in_source_chat from tasks inner join messages on tasks.source_message_id=messages.message_db_id inner join chats c on tasks.source_chat = c.chat_id
            where tasks.bot_id=get_bot_id(bot_token) and task_status<3;
    end;
$$ LANGUAGE plpgsql;