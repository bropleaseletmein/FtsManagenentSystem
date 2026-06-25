-- =============================================================
-- 004_triggers.sql  –  Trigger functions and triggers
-- =============================================================

-- ---------------------------------------------------------------
-- 1. Email normalization on clients
-- ---------------------------------------------------------------
CREATE OR REPLACE FUNCTION trg_normalize_client_email()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
BEGIN
    IF NEW.email IS NOT NULL THEN
        NEW.email := lower(trim(NEW.email));
    END IF;
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_clients_email_normalize
    BEFORE INSERT OR UPDATE ON clients
    FOR EACH ROW EXECUTE FUNCTION trg_normalize_client_email();

-- ---------------------------------------------------------------
-- 2. Email normalization on staff
-- ---------------------------------------------------------------
CREATE OR REPLACE FUNCTION trg_normalize_staff_email()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
BEGIN
    NEW.email := lower(trim(NEW.email));
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_staff_email_normalize
    BEFORE INSERT OR UPDATE ON staff
    FOR EACH ROW EXECUTE FUNCTION trg_normalize_staff_email();

-- Email normalization on staff_credentials
CREATE OR REPLACE FUNCTION trg_normalize_staff_cred_email()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
BEGIN
    NEW.email := lower(trim(NEW.email));
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_staff_credentials_email_normalize
    BEFORE INSERT OR UPDATE ON staff_credentials
    FOR EACH ROW EXECUTE FUNCTION trg_normalize_staff_cred_email();

-- Email normalization on client_credentials
CREATE OR REPLACE FUNCTION trg_normalize_client_cred_email()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
BEGIN
    NEW.email := lower(trim(NEW.email));
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_client_credentials_email_normalize
    BEFORE INSERT OR UPDATE ON client_credentials
    FOR EACH ROW EXECUTE FUNCTION trg_normalize_client_cred_email();

-- ---------------------------------------------------------------
-- 3. Subscription status change log
-- ---------------------------------------------------------------
CREATE OR REPLACE FUNCTION trg_log_subscription_status()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
BEGIN
    IF OLD.status IS DISTINCT FROM NEW.status THEN
        INSERT INTO subscription_status_log
            (id, client_subscription_id, old_status, new_status, changed_at)
        VALUES
            (gen_random_uuid(), NEW.id, OLD.status::varchar, NEW.status::varchar, now());
    END IF;
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_client_subscriptions_status_log
    AFTER UPDATE ON client_subscriptions
    FOR EACH ROW EXECUTE FUNCTION trg_log_subscription_status();

-- ---------------------------------------------------------------
-- 4. Booking status change log
-- ---------------------------------------------------------------
CREATE OR REPLACE FUNCTION trg_log_booking_status()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
BEGIN
    IF OLD.status IS DISTINCT FROM NEW.status THEN
        INSERT INTO booking_status_log
            (id, class_booking_id, old_status, new_status, changed_at)
        VALUES
            (gen_random_uuid(), NEW.id, OLD.status::varchar, NEW.status::varchar, now());
    END IF;
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_class_bookings_status_log
    AFTER UPDATE ON class_bookings
    FOR EACH ROW EXECUTE FUNCTION trg_log_booking_status();
