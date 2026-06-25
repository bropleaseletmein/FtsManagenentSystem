-- =============================================================
-- 001_schema.sql  –  DDL for FitnessNetwork
-- =============================================================

-- ---------- clubs ----------
CREATE TABLE clubs (
    id          uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    name        varchar     NOT NULL,
    address     varchar     NOT NULL,
    phone       varchar,
    deleted_at  timestamp   NULL
);

-- ---------- halls ----------
CREATE TABLE halls (
    id          uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    club_id     uuid        NOT NULL REFERENCES clubs(id),
    name        varchar     NOT NULL,
    capacity    int         NOT NULL,
    deleted_at  timestamp   NULL
);

-- ---------- staff ----------
CREATE TABLE staff (
    id          uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    club_id     uuid        NOT NULL REFERENCES clubs(id),
    first_name  varchar     NOT NULL,
    last_name   varchar     NOT NULL,
    email       varchar     NOT NULL,
    deleted_at  timestamp   NULL
);

-- ---------- staff_roles ----------
CREATE TYPE staff_role AS ENUM ('admin', 'trainer');

CREATE TABLE staff_roles (
    staff_id    uuid        NOT NULL REFERENCES staff(id),
    role        staff_role  NOT NULL,
    PRIMARY KEY (staff_id, role)
);

-- ---------- subscription_types ----------
CREATE TABLE subscription_types (
    id              uuid            PRIMARY KEY DEFAULT gen_random_uuid(),
    name            varchar         NOT NULL,
    duration_days   int,
    visits_limit    int,
    price           decimal(10,2)   NOT NULL,
    is_all_clubs    boolean         NOT NULL DEFAULT false,
    deleted_at      timestamp       NULL
);

-- ---------- subscription_type_clubs ----------
CREATE TABLE subscription_type_clubs (
    subscription_type_id    uuid    NOT NULL REFERENCES subscription_types(id),
    club_id                 uuid    NOT NULL REFERENCES clubs(id),
    PRIMARY KEY (subscription_type_id, club_id)
);

-- ---------- class_types ----------
CREATE TABLE class_types (
    id          uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    name        varchar     NOT NULL,
    description text,
    deleted_at  timestamp   NULL
);

-- ---------- clients ----------
CREATE TABLE clients (
    id          uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    first_name  varchar     NOT NULL,
    last_name   varchar     NOT NULL,
    email       varchar,
    phone       varchar,
    birth_date  date,
    deleted_at  timestamp   NULL
);

-- ---------- client_subscriptions ----------
CREATE TYPE subscription_status AS ENUM ('pending','active','frozen','expired','cancelled');

CREATE TABLE client_subscriptions (
    id                      uuid                    PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id               uuid                    NOT NULL REFERENCES clients(id),
    subscription_type_id    uuid                    NOT NULL REFERENCES subscription_types(id),
    status                  subscription_status     NOT NULL DEFAULT 'pending',
    started_at              timestamp,
    expires_at              timestamp,
    visits_left             int
);

-- ---------- subscription_freezes ----------
CREATE TABLE subscription_freezes (
    id                          uuid    PRIMARY KEY DEFAULT gen_random_uuid(),
    client_subscription_id      uuid    NOT NULL REFERENCES client_subscriptions(id),
    started_at                  date    NOT NULL,
    ended_at                    date,
    days_frozen                 int
);

-- ---------- subscription_status_log ----------
CREATE TABLE subscription_status_log (
    id                          uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    client_subscription_id      uuid        NOT NULL REFERENCES client_subscriptions(id),
    old_status                  varchar,
    new_status                  varchar     NOT NULL,
    changed_at                  timestamp   NOT NULL DEFAULT now()
);

-- ---------- staff_credentials ----------
CREATE TABLE staff_credentials (
    id              uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    staff_id        uuid        NOT NULL UNIQUE REFERENCES staff(id),
    email           varchar     NOT NULL UNIQUE,
    password_hash   varchar     NOT NULL,
    created_at      timestamp   NOT NULL DEFAULT now()
);

-- ---------- client_credentials ----------
CREATE TABLE client_credentials (
    id              uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id       uuid        NOT NULL UNIQUE REFERENCES clients(id),
    email           varchar     NOT NULL UNIQUE,
    password_hash   varchar     NOT NULL,
    created_at      timestamp   NOT NULL DEFAULT now()
);

-- ---------- visits ----------
CREATE TYPE entry_method AS ENUM ('card','qr','bracelet');

CREATE TABLE visits (
    id                          uuid            PRIMARY KEY DEFAULT gen_random_uuid(),
    club_id                     uuid            NOT NULL REFERENCES clubs(id),
    client_subscription_id      uuid            NOT NULL REFERENCES client_subscriptions(id),
    entry_method                entry_method    NOT NULL,
    entered_at                  timestamp       NOT NULL,
    exited_at                   timestamp
);

-- ---------- class_schedule ----------
CREATE TYPE class_status AS ENUM ('scheduled','cancelled','completed');

CREATE TABLE class_schedule (
    id              uuid            PRIMARY KEY DEFAULT gen_random_uuid(),
    class_type_id   uuid            NOT NULL REFERENCES class_types(id),
    hall_id         uuid            NOT NULL REFERENCES halls(id),
    trainer_id      uuid            NOT NULL REFERENCES staff(id),
    starts_at       timestamp       NOT NULL,
    ends_at         timestamp       NOT NULL,
    capacity        int             NOT NULL,
    status          class_status    NOT NULL DEFAULT 'scheduled'
);

-- ---------- class_bookings ----------
CREATE TYPE booking_status AS ENUM ('booked','cancelled');

CREATE TABLE class_bookings (
    id                      uuid            PRIMARY KEY DEFAULT gen_random_uuid(),
    client_subscription_id  uuid            NOT NULL REFERENCES client_subscriptions(id),
    class_schedule_id       uuid            NOT NULL REFERENCES class_schedule(id),
    status                  booking_status  NOT NULL DEFAULT 'booked',
    created_at              timestamp       NOT NULL DEFAULT now()
);

-- ---------- booking_status_log ----------
CREATE TABLE booking_status_log (
    id                  uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    class_booking_id    uuid        NOT NULL REFERENCES class_bookings(id),
    old_status          varchar,
    new_status          varchar     NOT NULL,
    changed_at          timestamp   NOT NULL DEFAULT now()
);
