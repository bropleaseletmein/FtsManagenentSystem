-- =============================================================
-- 002_indexes.sql  –  FK indexes + partial unique indexes
-- =============================================================

-- FK indexes (Postgres does not create them automatically)
CREATE INDEX ix_halls_club_id                       ON halls(club_id);
CREATE INDEX ix_staff_club_id                       ON staff(club_id);
CREATE INDEX ix_client_subscriptions_client_id      ON client_subscriptions(client_id);
CREATE INDEX ix_client_subscriptions_type_id        ON client_subscriptions(subscription_type_id);
CREATE INDEX ix_subscription_freezes_sub_id         ON subscription_freezes(client_subscription_id);
CREATE INDEX ix_visits_club_id                      ON visits(club_id);
CREATE INDEX ix_visits_client_subscription_id       ON visits(client_subscription_id);
CREATE INDEX ix_class_schedule_class_type_id        ON class_schedule(class_type_id);
CREATE INDEX ix_class_schedule_hall_id              ON class_schedule(hall_id);
CREATE INDEX ix_class_schedule_trainer_id           ON class_schedule(trainer_id);
CREATE INDEX ix_class_bookings_sub_id               ON class_bookings(client_subscription_id);
CREATE INDEX ix_class_bookings_schedule_id          ON class_bookings(class_schedule_id);
CREATE INDEX ix_sub_status_log_sub_id               ON subscription_status_log(client_subscription_id);
CREATE INDEX ix_booking_status_log_booking_id       ON booking_status_log(class_booking_id);

-- Partial unique indexes (to allow duplicates on soft-deleted rows)
CREATE UNIQUE INDEX ux_clients_email ON clients(email) WHERE deleted_at IS NULL;
CREATE UNIQUE INDEX ux_clients_phone ON clients(phone) WHERE deleted_at IS NULL;
CREATE UNIQUE INDEX ux_staff_email   ON staff(email)   WHERE deleted_at IS NULL;
