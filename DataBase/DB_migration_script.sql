create table public.responces (
    resp_id serial,
    bot_id int,
    calling_text text,
    mess_text text,
    primary key (resp_id),
    foreign key (bot_id) references public.bots (bot_id)
);

CREATE OR REPLACE FUNCTION add_responce(_calling_text text,_mess_text text, token text) RETURNS void as
$$
    begin
        delete FROM public.responces where calling_text=_calling_text and bot_id=get_bot_id(token);
        insert INTO public.responces (calling_text, mess_text, bot_id) values (_calling_text,_mess_text,get_bot_id(token));
    end;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION get_responce(_calling_text text, token text) RETURNS text as
$$
    declare for_ret text;
    begin
        for_ret = (select mess_text from public.responces where responces.calling_text=_calling_text and responces.bot_id=get_bot_id(token));
        return for_ret;
    end;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION del_responce(_calling_text text, token text) RETURNS void as
$$
    begin
        delete from public.responces where responces.calling_text=_calling_text and responces.bot_id=get_bot_id(token);
    end;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION get_callings(token text) RETURNS table (_calling_text text) as
$$
    begin
        RETURN QUERY select calling_text from public.responces where responces.bot_id=get_bot_id(token);
    end;
$$ LANGUAGE plpgsql;