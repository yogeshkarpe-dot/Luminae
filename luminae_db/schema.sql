-- ============================================================
-- LUMINAE - Aurora Art Community
-- 01_schema.sql  |  PostgreSQL 16
-- ============================================================

CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ─────────────────────────────────────────
--  USERS
-- ─────────────────────────────────────────
CREATE TABLE IF NOT EXISTS users (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    username        VARCHAR(50) NOT NULL UNIQUE,
    email           VARCHAR(255) NOT NULL UNIQUE,
    password_hash   TEXT        NOT NULL,
    display_name    VARCHAR(100),
    bio             TEXT,
    avatar_url      TEXT,
    banner_url      TEXT,
    website         VARCHAR(255),
    is_active       BOOLEAN     NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ─────────────────────────────────────────
--  POSTS
-- ─────────────────────────────────────────
CREATE TYPE media_type AS ENUM ('photography', 'digital_art', 'animation', '3d_render');

CREATE TABLE IF NOT EXISTS posts (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id         UUID        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    title           VARCHAR(200) NOT NULL,
    description     TEXT,
    media_url       TEXT        NOT NULL,
    thumbnail_url   TEXT,
    media_type      media_type  NOT NULL,
    tags            TEXT[]      DEFAULT '{}',
    is_published    BOOLEAN     NOT NULL DEFAULT TRUE,
    view_count      INT         NOT NULL DEFAULT 0,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ─────────────────────────────────────────
--  LIKES
-- ─────────────────────────────────────────
CREATE TABLE IF NOT EXISTS likes (
    id          UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id     UUID        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    post_id     UUID        NOT NULL REFERENCES posts(id) ON DELETE CASCADE,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (user_id, post_id)
);

-- ─────────────────────────────────────────
--  COMMENTS
-- ─────────────────────────────────────────
CREATE TABLE IF NOT EXISTS comments (
    id          UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id     UUID        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    post_id     UUID        NOT NULL REFERENCES posts(id) ON DELETE CASCADE,
    content     TEXT        NOT NULL CHECK (char_length(content) BETWEEN 1 AND 1000),
    is_active   BOOLEAN     NOT NULL DEFAULT TRUE,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ─────────────────────────────────────────
--  FOLLOWS
-- ─────────────────────────────────────────
CREATE TABLE IF NOT EXISTS follows (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    follower_id     UUID        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    following_id    UUID        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (follower_id, following_id),
    CHECK (follower_id <> following_id)
);

-- ─────────────────────────────────────────
--  CONVERSATIONS
-- ─────────────────────────────────────────
-- 1. Create the table without the broken UNIQUE constraint
CREATE TABLE IF NOT EXISTS conversations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    participant_a UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    participant_b UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    last_message_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CHECK (participant_a <> participant_b)
);

-- 2. Add the unique index to prevent duplicate conversations in both directions
CREATE UNIQUE INDEX IF NOT EXISTS idx_conversations_participants
    ON conversations (
        LEAST(participant_a, participant_b),
        GREATEST(participant_a, participant_b)
    );

-- ─────────────────────────────────────────
--  MESSAGES
-- ─────────────────────────────────────────
CREATE TABLE IF NOT EXISTS messages (
    id                  UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    conversation_id     UUID        NOT NULL REFERENCES conversations(id) ON DELETE CASCADE,
    sender_id           UUID        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    content             TEXT        NOT NULL CHECK (char_length(content) BETWEEN 1 AND 2000),
    is_read             BOOLEAN     NOT NULL DEFAULT FALSE,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ─────────────────────────────────────────
--  INDEXES
-- ─────────────────────────────────────────
CREATE INDEX IF NOT EXISTS idx_posts_user_id       ON posts(user_id);
CREATE INDEX IF NOT EXISTS idx_posts_media_type    ON posts(media_type);
CREATE INDEX IF NOT EXISTS idx_posts_created_at    ON posts(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_posts_tags          ON posts USING GIN(tags);
CREATE INDEX IF NOT EXISTS idx_likes_post_id       ON likes(post_id);
CREATE INDEX IF NOT EXISTS idx_likes_user_id       ON likes(user_id);
CREATE INDEX IF NOT EXISTS idx_comments_post_id    ON comments(post_id);
CREATE INDEX IF NOT EXISTS idx_follows_follower    ON follows(follower_id);
CREATE INDEX IF NOT EXISTS idx_follows_following   ON follows(following_id);
CREATE INDEX IF NOT EXISTS idx_messages_conv       ON messages(conversation_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_users_username      ON users(username);
CREATE INDEX IF NOT EXISTS idx_users_email         ON users(email);