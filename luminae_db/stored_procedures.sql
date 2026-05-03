-- ============================================================
-- LUMINAE - Aurora Art Community
-- 02_stored_procedures.sql  |  PostgreSQL 16
-- All data access is routine-driven (Dapper calls these)
-- ============================================================

-- ─────────────────────────────────────────
--  AUTH PROCEDURES
-- ─────────────────────────────────────────

-- 1. Register a new user
CREATE OR REPLACE FUNCTION sp_register_user(
    p_username      VARCHAR,
    p_email         VARCHAR,
    p_password_hash TEXT,
    p_display_name  VARCHAR DEFAULT NULL
)
RETURNS TABLE (
    id UUID, username VARCHAR, email VARCHAR, display_name VARCHAR,
    avatar_url TEXT, bio TEXT, created_at TIMESTAMPTZ, error_message TEXT
)
LANGUAGE plpgsql AS $$
BEGIN
    IF EXISTS (SELECT 1 FROM users u WHERE u.username = p_username) THEN
        RETURN QUERY SELECT NULL::UUID, NULL::VARCHAR, NULL::VARCHAR, NULL::VARCHAR,
                            NULL::TEXT, NULL::TEXT, NULL::TIMESTAMPTZ, 'Username already taken'::TEXT;
        RETURN;
    END IF;

    IF EXISTS (SELECT 1 FROM users u WHERE u.email = p_email) THEN
        RETURN QUERY SELECT NULL::UUID, NULL::VARCHAR, NULL::VARCHAR, NULL::VARCHAR,
                            NULL::TEXT, NULL::TEXT, NULL::TIMESTAMPTZ, 'Email already registered'::TEXT;
        RETURN;
    END IF;

    RETURN QUERY
    INSERT INTO users (username, email, password_hash, display_name)
    VALUES (p_username, p_email, p_password_hash, COALESCE(p_display_name, p_username))
    RETURNING users.id, users.username, users.email, users.display_name,
              users.avatar_url, users.bio, users.created_at, NULL::TEXT;
END;
$$;

-- 2. Login — returns user + hash for bcrypt verification
CREATE OR REPLACE FUNCTION sp_login_user(p_email VARCHAR)
RETURNS TABLE (
    id UUID, username VARCHAR, email VARCHAR, display_name VARCHAR,
    avatar_url TEXT, bio TEXT, banner_url TEXT, password_hash TEXT, is_active BOOLEAN
)
LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    SELECT u.id, u.username, u.email, u.display_name,
           u.avatar_url, u.bio, u.banner_url, u.password_hash, u.is_active
    FROM users u
    WHERE u.email = p_email;
END;
$$;

-- ─────────────────────────────────────────
--  USER / PROFILE PROCEDURES
-- ─────────────────────────────────────────

-- 3. Get user profile by username
CREATE OR REPLACE FUNCTION sp_get_user_profile(
    p_username      VARCHAR,
    p_viewer_id     UUID DEFAULT NULL
)
RETURNS TABLE (
    id UUID, username VARCHAR, display_name VARCHAR, bio TEXT,
    avatar_url TEXT, banner_url TEXT, website VARCHAR,
    post_count BIGINT, follower_count BIGINT, following_count BIGINT,
    is_following BOOLEAN, created_at TIMESTAMPTZ
)
LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    SELECT u.id, u.username, u.display_name, u.bio,
           u.avatar_url, u.banner_url, u.website,
           COUNT(DISTINCT p.id)::BIGINT,
           COUNT(DISTINCT f1.id)::BIGINT,
           COUNT(DISTINCT f2.id)::BIGINT,
           CASE WHEN p_viewer_id IS NOT NULL
                THEN EXISTS(SELECT 1 FROM follows WHERE follower_id = p_viewer_id AND following_id = u.id)
                ELSE FALSE END,
           u.created_at
    FROM users u
    LEFT JOIN posts p ON p.user_id = u.id AND p.is_published = TRUE
    LEFT JOIN follows f1 ON f1.following_id = u.id
    LEFT JOIN follows f2 ON f2.follower_id = u.id
    WHERE u.username = p_username AND u.is_active = TRUE
    GROUP BY u.id;
END;
$$;

-- 4. Update user profile
CREATE OR REPLACE FUNCTION sp_update_user_profile(
    p_user_id       UUID,
    p_display_name  VARCHAR DEFAULT NULL,
    p_bio           TEXT DEFAULT NULL,
    p_avatar_url    TEXT DEFAULT NULL,
    p_banner_url    TEXT DEFAULT NULL,
    p_website       VARCHAR DEFAULT NULL
)
RETURNS TABLE (
    id UUID, username VARCHAR, display_name VARCHAR, bio TEXT,
    avatar_url TEXT, banner_url TEXT, website VARCHAR, updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    UPDATE users
    SET display_name = COALESCE(p_display_name, display_name),
        bio          = COALESCE(p_bio, bio),
        avatar_url   = COALESCE(p_avatar_url, avatar_url),
        banner_url   = COALESCE(p_banner_url, banner_url),
        website      = COALESCE(p_website, website),
        updated_at   = NOW()
    WHERE id = p_user_id
    RETURNING users.id, users.username, users.display_name, users.bio,
              users.avatar_url, users.banner_url, users.website, users.updated_at;
END;
$$;

-- 5. Search users
CREATE OR REPLACE FUNCTION sp_search_users(p_query VARCHAR, p_limit INT DEFAULT 10)
RETURNS TABLE (
    id UUID, username VARCHAR, display_name VARCHAR, avatar_url TEXT, bio TEXT
)
LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    SELECT u.id, u.username, u.display_name, u.avatar_url, u.bio
    FROM users u
    WHERE u.is_active = TRUE
      AND (u.username ILIKE '%' || p_query || '%'
           OR u.display_name ILIKE '%' || p_query || '%')
    ORDER BY u.username
    LIMIT p_limit;
END;
$$;

-- ─────────────────────────────────────────
--  POST PROCEDURES
-- ─────────────────────────────────────────

-- 6. Get public feed (paginated, newest first)
CREATE OR REPLACE FUNCTION sp_get_feed(
    p_viewer_id     UUID DEFAULT NULL,
    p_media_type    VARCHAR DEFAULT NULL,
    p_offset        INT DEFAULT 0,
    p_limit         INT DEFAULT 20
)
RETURNS TABLE (
    id UUID, title VARCHAR, description TEXT, media_url TEXT,
    thumbnail_url TEXT, media_type media_type, tags TEXT[],
    view_count INT, created_at TIMESTAMPTZ,
    author_id UUID, author_username VARCHAR,
    author_display_name VARCHAR, author_avatar_url TEXT,
    like_count BIGINT, comment_count BIGINT, is_liked BOOLEAN
)
LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    SELECT p.id, p.title, p.description, p.media_url,
           p.thumbnail_url, p.media_type, p.tags,
           p.view_count, p.created_at,
           u.id, u.username, u.display_name, u.avatar_url,
           COUNT(DISTINCT l.id)::BIGINT,
           COUNT(DISTINCT c.id)::BIGINT,
           CASE WHEN p_viewer_id IS NOT NULL
                THEN EXISTS(SELECT 1 FROM likes WHERE user_id = p_viewer_id AND post_id = p.id)
                ELSE FALSE END
    FROM posts p
    JOIN users u ON u.id = p.user_id
    LEFT JOIN likes l ON l.post_id = p.id
    LEFT JOIN comments c ON c.post_id = p.id AND c.is_active = TRUE
    WHERE p.is_published = TRUE
      AND u.is_active = TRUE
      AND (p_media_type IS NULL OR p.media_type::TEXT = p_media_type)
    GROUP BY p.id, u.id
    ORDER BY p.created_at DESC
    OFFSET p_offset
    LIMIT p_limit;
END;
$$;

-- 7. Get single post by ID
CREATE OR REPLACE FUNCTION sp_get_post_by_id(p_post_id UUID, p_viewer_id UUID DEFAULT NULL)
RETURNS TABLE (
    id UUID, title VARCHAR, description TEXT, media_url TEXT,
    thumbnail_url TEXT, media_type media_type, tags TEXT[],
    view_count INT, created_at TIMESTAMPTZ,
    author_id UUID, author_username VARCHAR,
    author_display_name VARCHAR, author_avatar_url TEXT,
    like_count BIGINT, comment_count BIGINT, is_liked BOOLEAN
)
LANGUAGE plpgsql AS $$
BEGIN
    -- increment view count
    UPDATE posts SET view_count = view_count + 1 WHERE posts.id = p_post_id;

    RETURN QUERY
    SELECT p.id, p.title, p.description, p.media_url,
           p.thumbnail_url, p.media_type, p.tags,
           p.view_count, p.created_at,
           u.id, u.username, u.display_name, u.avatar_url,
           COUNT(DISTINCT l.id)::BIGINT,
           COUNT(DISTINCT c.id)::BIGINT,
           CASE WHEN p_viewer_id IS NOT NULL
                THEN EXISTS(SELECT 1 FROM likes WHERE user_id = p_viewer_id AND post_id = p.id)
                ELSE FALSE END
    FROM posts p
    JOIN users u ON u.id = p.user_id
    LEFT JOIN likes l ON l.post_id = p.id
    LEFT JOIN comments c ON c.post_id = p.id AND c.is_active = TRUE
    WHERE p.id = p_post_id AND p.is_published = TRUE
    GROUP BY p.id, u.id;
END;
$$;

-- 8. Get posts by user
CREATE OR REPLACE FUNCTION sp_get_user_posts(
    p_profile_user_id   UUID,
    p_viewer_id         UUID DEFAULT NULL,
    p_offset            INT DEFAULT 0,
    p_limit             INT DEFAULT 20
)
RETURNS TABLE (
    id UUID, title VARCHAR, media_url TEXT, thumbnail_url TEXT,
    media_type media_type, view_count INT, created_at TIMESTAMPTZ,
    like_count BIGINT, comment_count BIGINT, is_liked BOOLEAN
)
LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    SELECT p.id, p.title, p.media_url, p.thumbnail_url,
           p.media_type, p.view_count, p.created_at,
           COUNT(DISTINCT l.id)::BIGINT,
           COUNT(DISTINCT c.id)::BIGINT,
           CASE WHEN p_viewer_id IS NOT NULL
                THEN EXISTS(SELECT 1 FROM likes WHERE user_id = p_viewer_id AND post_id = p.id)
                ELSE FALSE END
    FROM posts p
    LEFT JOIN likes l ON l.post_id = p.id
    LEFT JOIN comments c ON c.post_id = p.id AND c.is_active = TRUE
    WHERE p.user_id = p_profile_user_id AND p.is_published = TRUE
    GROUP BY p.id
    ORDER BY p.created_at DESC
    OFFSET p_offset LIMIT p_limit;
END;
$$;

-- 9. Create post
CREATE OR REPLACE FUNCTION sp_create_post(
    p_user_id       UUID,
    p_title         VARCHAR,
    p_description   TEXT DEFAULT NULL,
    p_media_url     TEXT DEFAULT NULL,
    p_thumbnail_url TEXT DEFAULT NULL,
    p_media_type    VARCHAR DEFAULT 'photography',
    p_tags          TEXT[] DEFAULT '{}'
)
RETURNS TABLE (id UUID, title VARCHAR, media_url TEXT, media_type media_type, created_at TIMESTAMPTZ)
LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    INSERT INTO posts (user_id, title, description, media_url, thumbnail_url, media_type, tags)
    VALUES (p_user_id, p_title, p_description, p_media_url, p_thumbnail_url,
            p_media_type::media_type, p_tags)
    RETURNING posts.id, posts.title, posts.media_url, posts.media_type, posts.created_at;
END;
$$;

-- 10. Delete post (soft via unpublish)
CREATE OR REPLACE FUNCTION sp_delete_post(p_post_id UUID, p_user_id UUID)
RETURNS TABLE (success BOOLEAN, message TEXT)
LANGUAGE plpgsql AS $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM posts WHERE id = p_post_id AND user_id = p_user_id) THEN
        RETURN QUERY SELECT FALSE, 'Post not found or not yours'::TEXT;
        RETURN;
    END IF;

    UPDATE posts SET is_published = FALSE, updated_at = NOW()
    WHERE id = p_post_id AND user_id = p_user_id;
    RETURN QUERY SELECT TRUE, 'Post deleted'::TEXT;
END;
$$;

-- 11. Search posts by tag or keyword
CREATE OR REPLACE FUNCTION sp_search_posts(
    p_query     VARCHAR,
    p_offset    INT DEFAULT 0,
    p_limit     INT DEFAULT 20
)
RETURNS TABLE (
    id UUID, title VARCHAR, media_url TEXT, thumbnail_url TEXT,
    media_type media_type, created_at TIMESTAMPTZ,
    author_username VARCHAR, author_avatar_url TEXT, like_count BIGINT
)
LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    SELECT p.id, p.title, p.media_url, p.thumbnail_url,
           p.media_type, p.created_at,
           u.username, u.avatar_url,
           COUNT(l.id)::BIGINT
    FROM posts p
    JOIN users u ON u.id = p.user_id
    LEFT JOIN likes l ON l.post_id = p.id
    WHERE p.is_published = TRUE AND u.is_active = TRUE
      AND (p.title ILIKE '%' || p_query || '%'
           OR p.description ILIKE '%' || p_query || '%'
           OR p_query = ANY(p.tags))
    GROUP BY p.id, u.id
    ORDER BY p.created_at DESC
    OFFSET p_offset LIMIT p_limit;
END;
$$;

-- ─────────────────────────────────────────
--  LIKES PROCEDURES
-- ─────────────────────────────────────────

-- 12. Toggle like (like if not liked, unlike if liked)
CREATE OR REPLACE FUNCTION sp_toggle_like(p_user_id UUID, p_post_id UUID)
RETURNS TABLE (liked BOOLEAN, like_count BIGINT)
LANGUAGE plpgsql AS $$
DECLARE
    v_liked BOOLEAN;
BEGIN
    IF EXISTS (SELECT 1 FROM likes WHERE user_id = p_user_id AND post_id = p_post_id) THEN
        DELETE FROM likes WHERE user_id = p_user_id AND post_id = p_post_id;
        v_liked := FALSE;
    ELSE
        INSERT INTO likes (user_id, post_id) VALUES (p_user_id, p_post_id);
        v_liked := TRUE;
    END IF;

    RETURN QUERY
    SELECT v_liked, COUNT(*)::BIGINT FROM likes WHERE post_id = p_post_id;
END;
$$;

-- ─────────────────────────────────────────
--  COMMENTS PROCEDURES
-- ─────────────────────────────────────────

-- 13. Add comment
CREATE OR REPLACE FUNCTION sp_add_comment(
    p_user_id   UUID,
    p_post_id   UUID,
    p_content   TEXT
)
RETURNS TABLE (
    id UUID, content TEXT, created_at TIMESTAMPTZ,
    author_username VARCHAR, author_avatar_url TEXT, author_display_name VARCHAR
)
LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    WITH new_comment AS (
        INSERT INTO comments (user_id, post_id, content)
        VALUES (p_user_id, p_post_id, p_content)
        RETURNING comments.id, comments.content, comments.created_at, comments.user_id
    )
    SELECT nc.id, nc.content, nc.created_at, u.username, u.avatar_url, u.display_name
    FROM new_comment nc
    JOIN users u ON u.id = nc.user_id;
END;
$$;

-- 14. Get comments for a post
CREATE OR REPLACE FUNCTION sp_get_post_comments(
    p_post_id   UUID,
    p_offset    INT DEFAULT 0,
    p_limit     INT DEFAULT 30
)
RETURNS TABLE (
    id UUID, content TEXT, created_at TIMESTAMPTZ,
    author_id UUID, author_username VARCHAR,
    author_display_name VARCHAR, author_avatar_url TEXT
)
LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    SELECT c.id, c.content, c.created_at,
           u.id, u.username, u.display_name, u.avatar_url
    FROM comments c
    JOIN users u ON u.id = c.user_id
    WHERE c.post_id = p_post_id AND c.is_active = TRUE
    ORDER BY c.created_at ASC
    OFFSET p_offset LIMIT p_limit;
END;
$$;

-- 15. Delete comment
CREATE OR REPLACE FUNCTION sp_delete_comment(p_comment_id UUID, p_user_id UUID)
RETURNS BOOLEAN
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE comments SET is_active = FALSE
    WHERE id = p_comment_id AND user_id = p_user_id;
    RETURN FOUND;
END;
$$;

-- ─────────────────────────────────────────
--  FOLLOW PROCEDURES
-- ─────────────────────────────────────────

-- 16. Toggle follow
CREATE OR REPLACE FUNCTION sp_toggle_follow(p_follower_id UUID, p_following_id UUID)
RETURNS TABLE (is_following BOOLEAN, follower_count BIGINT)
LANGUAGE plpgsql AS $$
DECLARE
    v_following BOOLEAN;
BEGIN
    IF EXISTS (SELECT 1 FROM follows WHERE follower_id = p_follower_id AND following_id = p_following_id) THEN
        DELETE FROM follows WHERE follower_id = p_follower_id AND following_id = p_following_id;
        v_following := FALSE;
    ELSE
        INSERT INTO follows (follower_id, following_id) VALUES (p_follower_id, p_following_id);
        v_following := TRUE;
    END IF;

    RETURN QUERY
    SELECT v_following, COUNT(*)::BIGINT FROM follows WHERE following_id = p_following_id;
END;
$$;

-- 17. Get followers list
CREATE OR REPLACE FUNCTION sp_get_followers(p_user_id UUID, p_limit INT DEFAULT 50)
RETURNS TABLE (id UUID, username VARCHAR, display_name VARCHAR, avatar_url TEXT)
LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    SELECT u.id, u.username, u.display_name, u.avatar_url
    FROM follows f
    JOIN users u ON u.id = f.follower_id
    WHERE f.following_id = p_user_id AND u.is_active = TRUE
    ORDER BY f.created_at DESC
    LIMIT p_limit;
END;
$$;

-- 18. Get following list
CREATE OR REPLACE FUNCTION sp_get_following(p_user_id UUID, p_limit INT DEFAULT 50)
RETURNS TABLE (id UUID, username VARCHAR, display_name VARCHAR, avatar_url TEXT)
LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    SELECT u.id, u.username, u.display_name, u.avatar_url
    FROM follows f
    JOIN users u ON u.id = f.following_id
    WHERE f.follower_id = p_user_id AND u.is_active = TRUE
    ORDER BY f.created_at DESC
    LIMIT p_limit;
END;
$$;

-- ─────────────────────────────────────────
--  MESSAGING PROCEDURES
-- ─────────────────────────────────────────

-- 19. Get or create conversation between two users
CREATE OR REPLACE FUNCTION sp_get_or_create_conversation(p_user_a UUID, p_user_b UUID)
RETURNS TABLE (id UUID, participant_a UUID, participant_b UUID, created_at TIMESTAMPTZ)
LANGUAGE plpgsql AS $$
DECLARE
    v_a UUID := LEAST(p_user_a::text, p_user_b::text)::UUID;
    v_b UUID := GREATEST(p_user_a::text, p_user_b::text)::UUID;
BEGIN
    INSERT INTO conversations (participant_a, participant_b)
    VALUES (v_a, v_b)
    ON CONFLICT DO NOTHING;

    RETURN QUERY
    SELECT c.id, c.participant_a, c.participant_b, c.created_at
    FROM conversations c
    WHERE c.participant_a = v_a AND c.participant_b = v_b;
END;
$$;

-- 20. Send a message
CREATE OR REPLACE FUNCTION sp_send_message(
    p_conversation_id   UUID,
    p_sender_id         UUID,
    p_content           TEXT
)
RETURNS TABLE (
    id UUID, content TEXT, is_read BOOLEAN,
    created_at TIMESTAMPTZ, sender_id UUID
)
LANGUAGE plpgsql AS $$
BEGIN
    -- Verify sender is a participant
    IF NOT EXISTS (
        SELECT 1 FROM conversations
        WHERE id = p_conversation_id
          AND (participant_a = p_sender_id OR participant_b = p_sender_id)
    ) THEN
        RAISE EXCEPTION 'Sender is not a participant in this conversation';
    END IF;

    UPDATE conversations SET last_message_at = NOW() WHERE id = p_conversation_id;

    RETURN QUERY
    INSERT INTO messages (conversation_id, sender_id, content)
    VALUES (p_conversation_id, p_sender_id, p_content)
    RETURNING messages.id, messages.content, messages.is_read,
              messages.created_at, messages.sender_id;
END;
$$;

-- 21. Get messages in a conversation
CREATE OR REPLACE FUNCTION sp_get_conversation_messages(
    p_conversation_id   UUID,
    p_user_id           UUID,
    p_offset            INT DEFAULT 0,
    p_limit             INT DEFAULT 50
)
RETURNS TABLE (
    id UUID, content TEXT, is_read BOOLEAN,
    created_at TIMESTAMPTZ, sender_id UUID,
    sender_username VARCHAR, sender_avatar_url TEXT
)
LANGUAGE plpgsql AS $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM conversations
        WHERE id = p_conversation_id
          AND (participant_a = p_user_id OR participant_b = p_user_id)
    ) THEN
        RAISE EXCEPTION 'Access denied';
    END IF;

    -- Mark messages as read
    UPDATE messages SET is_read = TRUE
    WHERE conversation_id = p_conversation_id AND sender_id <> p_user_id AND is_read = FALSE;

    RETURN QUERY
    SELECT m.id, m.content, m.is_read, m.created_at, m.sender_id,
           u.username, u.avatar_url
    FROM messages m
    JOIN users u ON u.id = m.sender_id
    WHERE m.conversation_id = p_conversation_id
    ORDER BY m.created_at ASC
    OFFSET p_offset LIMIT p_limit;
END;
$$;

-- 22. Get all conversations for a user
CREATE OR REPLACE FUNCTION sp_get_user_conversations(p_user_id UUID)
RETURNS TABLE (
    conversation_id UUID, other_user_id UUID,
    other_username VARCHAR, other_display_name VARCHAR, other_avatar_url TEXT,
    last_message TEXT, last_message_at TIMESTAMPTZ, unread_count BIGINT
)
LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    SELECT c.id,
           CASE WHEN c.participant_a = p_user_id THEN c.participant_b ELSE c.participant_a END,
           u.username, u.display_name, u.avatar_url,
           (SELECT m.content FROM messages m WHERE m.conversation_id = c.id
            ORDER BY m.created_at DESC LIMIT 1),
           c.last_message_at,
           COUNT(m2.id) FILTER (WHERE m2.is_read = FALSE AND m2.sender_id <> p_user_id)::BIGINT
    FROM conversations c
    JOIN users u ON u.id = CASE WHEN c.participant_a = p_user_id
                                THEN c.participant_b ELSE c.participant_a END
    LEFT JOIN messages m2 ON m2.conversation_id = c.id
    WHERE c.participant_a = p_user_id OR c.participant_b = p_user_id
    GROUP BY c.id, u.id
    ORDER BY c.last_message_at DESC NULLS LAST;
END;
$$;