CREATE DATABASE IF NOT EXISTS lab14pm
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_0900_ai_ci;
USE lab14pm;

CREATE TABLE IF NOT EXISTS roles (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  name VARCHAR(40) NOT NULL,
  description VARCHAR(180) NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uq_roles_name (name)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS users (
  id CHAR(36) NOT NULL,
  display_name VARCHAR(100) NOT NULL,
  email VARCHAR(254) NOT NULL,
  password_hash VARCHAR(255) NOT NULL COMMENT 'Hash de ejemplo; reemplazar si se implementa autenticación.',
  is_active BOOLEAN NOT NULL DEFAULT TRUE,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (id),
  UNIQUE KEY uq_users_email (email)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS user_roles (
  user_id CHAR(36) NOT NULL,
  role_id BIGINT UNSIGNED NOT NULL,
  PRIMARY KEY (user_id, role_id),
  CONSTRAINT fk_user_roles_user FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE,
  CONSTRAINT fk_user_roles_role FOREIGN KEY (role_id) REFERENCES roles (id) ON DELETE RESTRICT
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS tickets (
  id CHAR(36) NOT NULL,
  title VARCHAR(160) NOT NULL,
  description TEXT NOT NULL,
  status ENUM('open', 'in_progress', 'resolved', 'closed') NOT NULL DEFAULT 'open',
  created_by_user_id CHAR(36) NOT NULL,
  assigned_to_user_id CHAR(36) NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (id),
  KEY ix_tickets_status_created (status, created_at),
  KEY ix_tickets_assigned_to (assigned_to_user_id),
  CONSTRAINT fk_tickets_creator FOREIGN KEY (created_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
  CONSTRAINT fk_tickets_assignee FOREIGN KEY (assigned_to_user_id) REFERENCES users (id) ON DELETE SET NULL
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS ticket_responses (
  id CHAR(36) NOT NULL,
  ticket_id CHAR(36) NOT NULL,
  user_id CHAR(36) NOT NULL,
  message TEXT NOT NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (id),
  KEY ix_ticket_responses_ticket_created (ticket_id, created_at),
  CONSTRAINT fk_ticket_responses_ticket FOREIGN KEY (ticket_id) REFERENCES tickets (id) ON DELETE CASCADE,
  CONSTRAINT fk_ticket_responses_user FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT
) ENGINE=InnoDB;

INSERT INTO roles (name, description) VALUES
  ('admin', 'Administración del sistema'),
  ('agent', 'Atención y resolución de tickets'),
  ('requester', 'Persona que solicita atención')
ON DUPLICATE KEY UPDATE description = VALUES(description);

-- Registros de demostración. El password_hash es ficticio y no permite iniciar sesión.
INSERT INTO users (id, display_name, email, password_hash) VALUES
  ('11111111-1111-4111-8111-111111111111', 'Usuario Demo', 'demo@lab14pm.local', 'NO-ES-UN-HASH-VALIDO'),
  ('22222222-2222-4222-8222-222222222222', 'Agente Demo', 'agente@lab14pm.local', 'NO-ES-UN-HASH-VALIDO')
ON DUPLICATE KEY UPDATE display_name = VALUES(display_name);

INSERT INTO user_roles (user_id, role_id)
SELECT '11111111-1111-4111-8111-111111111111', id FROM roles WHERE name = 'requester'
ON DUPLICATE KEY UPDATE role_id = VALUES(role_id);
INSERT INTO user_roles (user_id, role_id)
SELECT '22222222-2222-4222-8222-222222222222', id FROM roles WHERE name = 'agent'
ON DUPLICATE KEY UPDATE role_id = VALUES(role_id);

INSERT INTO tickets (id, title, description, status, created_by_user_id, assigned_to_user_id) VALUES
  ('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa', 'Acceso al portal', 'No puedo ingresar al portal de demostración.', 'open', '11111111-1111-4111-8111-111111111111', '22222222-2222-4222-8222-222222222222'),
  ('bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb', 'Consulta de facturación', 'Solicito revisar el detalle de mi última factura.', 'in_progress', '11111111-1111-4111-8111-111111111111', '22222222-2222-4222-8222-222222222222')
ON DUPLICATE KEY UPDATE title = VALUES(title), description = VALUES(description), status = VALUES(status);

INSERT INTO ticket_responses (id, ticket_id, user_id, message) VALUES
  ('cccccccc-cccc-4ccc-8ccc-cccccccccccc', 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa', '22222222-2222-4222-8222-222222222222', 'Estamos revisando el acceso de demostración.')
ON DUPLICATE KEY UPDATE message = VALUES(message);

