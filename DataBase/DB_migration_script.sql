drop function  task_complited(source_chat_id bigint, target_chat_id bigint, bot_token text);
drop function  task_not_complited(source_chat_id bigint, target_chat_id bigint, bot_token text);

CREATE OR REPLACE FUNCTION task_complited(source_chat_id bigint,_source_message_id bigint,target_chat_id bigint,bot_token text ) RETURNS void as
$$
    begin
        update tasks set task_status=3 where source_chat=source_chat_id and source_message_id=_source_message_id and target_chat=target_chat_id and bot_id=get_bot_id(bot_token);
    end;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION task_not_complited(source_chat_id bigint,_source_message_id bigint,target_chat_id bigint,bot_token text ) RETURNS void as
$$
    begin
        update tasks set task_status=2 where source_chat=source_chat_id and source_message_id=_source_message_id and target_chat=target_chat_id and bot_id=get_bot_id(bot_token);
    end;
$$ LANGUAGE plpgsql;