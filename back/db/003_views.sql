-- =============================================================
-- 003_views.sql  –  Views
-- =============================================================

-- Active client subscriptions with extended info
CREATE OR REPLACE VIEW active_client_subscriptions AS
SELECT
    cs.id                       AS subscription_id,
    c.id                        AS client_id,
    c.first_name                AS client_first_name,
    c.last_name                 AS client_last_name,
    c.email                     AS client_email,
    c.phone                     AS client_phone,
    st.id                       AS subscription_type_id,
    st.name                     AS subscription_type_name,
    st.price                    AS subscription_price,
    cs.status,
    cs.started_at,
    cs.expires_at,
    cs.visits_left
FROM client_subscriptions cs
JOIN clients c  ON c.id = cs.client_id
JOIN subscription_types st ON st.id = cs.subscription_type_id
WHERE cs.status = 'active';

-- Who is currently in the club (no exit recorded yet)
CREATE OR REPLACE VIEW club_current_occupancy AS
SELECT
    v.id                AS visit_id,
    v.club_id,
    cl.name             AS club_name,
    c.id                AS client_id,
    c.first_name        AS client_first_name,
    c.last_name         AS client_last_name,
    v.entry_method,
    v.entered_at
FROM visits v
JOIN client_subscriptions cs ON cs.id = v.client_subscription_id
JOIN clients c               ON c.id  = cs.client_id
JOIN clubs cl                ON cl.id = v.club_id
WHERE v.exited_at IS NULL;
