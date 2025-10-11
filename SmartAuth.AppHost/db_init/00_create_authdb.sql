DO $$
BEGIN
   IF NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = 'authdb') THEN
      PERFORM dblink_exec('dbname=postgres', 'CREATE DATABASE authdb TEMPLATE template0');
END IF;
END$$;