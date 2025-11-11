-- AI Vetting System Database Schema

CREATE TABLE IF NOT EXISTS clients (
    id INT AUTO_INCREMENT PRIMARY KEY,
    email VARCHAR(255) UNIQUE NOT NULL,
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    status ENUM('pending', 'approved', 'rejected', 'in_progress') DEFAULT 'pending',
    overall_score DECIMAL(5,2) DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_status (status),
    INDEX idx_email (email)
);

CREATE TABLE IF NOT EXISTS conversations (
    id INT AUTO_INCREMENT PRIMARY KEY,
    client_id INT NOT NULL,
    started_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    ended_at TIMESTAMP NULL,
    status ENUM('active', 'completed', 'abandoned') DEFAULT 'active',
    total_messages INT DEFAULT 0,
    FOREIGN KEY (client_id) REFERENCES clients(id) ON DELETE CASCADE,
    INDEX idx_client (client_id),
    INDEX idx_status (status)
);

CREATE TABLE IF NOT EXISTS messages (
    id INT AUTO_INCREMENT PRIMARY KEY,
    conversation_id INT NOT NULL,
    role ENUM('user', 'assistant', 'system') NOT NULL,
    content TEXT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    tokens_used INT DEFAULT 0,
    FOREIGN KEY (conversation_id) REFERENCES conversations(id) ON DELETE CASCADE,
    INDEX idx_conversation (conversation_id)
);

CREATE TABLE IF NOT EXISTS dossier_entries (
    id INT AUTO_INCREMENT PRIMARY KEY,
    client_id INT NOT NULL,
    category ENUM('personal_life', 'business_life', 'family', 'childhood', 'education', 'values', 'goals', 'background', 'financial', 'other') NOT NULL,
    key_name VARCHAR(255) NOT NULL,
    value TEXT NOT NULL,
    confidence_score DECIMAL(5,2) DEFAULT 0,
    source_message_id INT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (client_id) REFERENCES clients(id) ON DELETE CASCADE,
    FOREIGN KEY (source_message_id) REFERENCES messages(id) ON DELETE SET NULL,
    INDEX idx_client (client_id),
    INDEX idx_category (category)
);

CREATE TABLE IF NOT EXISTS questions (
    id INT AUTO_INCREMENT PRIMARY KEY,
    question_text TEXT NOT NULL,
    category VARCHAR(100) NOT NULL,
    priority INT DEFAULT 0,
    is_required BOOLEAN DEFAULT false,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_category (category),
    INDEX idx_priority (priority)
);

CREATE TABLE IF NOT EXISTS asked_questions (
    id INT AUTO_INCREMENT PRIMARY KEY,
    conversation_id INT NOT NULL,
    question_id INT NOT NULL,
    asked_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    answered BOOLEAN DEFAULT false,
    FOREIGN KEY (conversation_id) REFERENCES conversations(id) ON DELETE CASCADE,
    FOREIGN KEY (question_id) REFERENCES questions(id) ON DELETE CASCADE,
    UNIQUE KEY unique_conversation_question (conversation_id, question_id)
);

CREATE TABLE IF NOT EXISTS criteria (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    category VARCHAR(100),
    weight DECIMAL(5,2) DEFAULT 1.0,
    is_active BOOLEAN DEFAULT true,
    evaluation_prompt TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_category (category)
);

CREATE TABLE IF NOT EXISTS red_flags (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    severity ENUM('low', 'medium', 'high', 'critical') DEFAULT 'medium',
    is_active BOOLEAN DEFAULT true,
    detection_keywords TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_severity (severity)
);

CREATE TABLE IF NOT EXISTS red_flag_detections (
    id INT AUTO_INCREMENT PRIMARY KEY,
    client_id INT NOT NULL,
    red_flag_id INT NOT NULL,
    message_id INT,
    detection_reason TEXT,
    confidence_score DECIMAL(5,2) DEFAULT 0,
    detected_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (client_id) REFERENCES clients(id) ON DELETE CASCADE,
    FOREIGN KEY (red_flag_id) REFERENCES red_flags(id) ON DELETE CASCADE,
    FOREIGN KEY (message_id) REFERENCES messages(id) ON DELETE SET NULL,
    INDEX idx_client (client_id)
);

CREATE TABLE IF NOT EXISTS system_settings (
    setting_key VARCHAR(255) PRIMARY KEY,
    setting_value TEXT,
    description TEXT,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- Insert default system settings
INSERT INTO system_settings (setting_key, setting_value, description) VALUES
('ai_api_provider', 'openai', 'AI provider: openai, anthropic, etc.'),
('ai_model', 'gpt-4', 'AI model to use for conversations'),
('ai_temperature', '0.7', 'Temperature for AI responses (0-1)'),
('ai_max_tokens', '500', 'Max tokens per AI response'),
('system_prompt', 'You are a professional interviewer conducting a thorough vetting conversation. Be friendly, empathetic, and ask follow-up questions naturally.', 'Base system prompt for AI'),
('min_messages_threshold', '20', 'Minimum messages before evaluation'),
('auto_evaluate', 'true', 'Automatically evaluate client after conversation ends')
ON DUPLICATE KEY UPDATE setting_value=VALUES(setting_value);

-- Insert default questions
INSERT INTO questions (question_text, category, priority, is_required) VALUES
('Can you tell me about your current business or professional situation?', 'business_life', 10, true),
('What are your main goals for seeking our services?', 'goals', 10, true),
('Tell me about your family and personal life.', 'personal_life', 8, true),
('What was your childhood like? Where did you grow up?', 'childhood', 7, false),
('What is your educational background?', 'education', 7, true),
('What are your core values?', 'values', 9, true),
('Can you describe your financial situation?', 'financial', 8, true),
('Have you worked with similar services before?', 'background', 6, false),
('What challenges are you currently facing?', 'business_life', 8, true),
('Who are the most important people in your life?', 'family', 7, false)
ON DUPLICATE KEY UPDATE question_text=VALUES(question_text);

-- Insert default criteria
INSERT INTO criteria (name, description, category, weight, evaluation_prompt) VALUES
('Financial Stability', 'Assess the client''s financial situation and stability', 'financial', 1.5, 'Evaluate the client''s financial stability based on their statements about income, assets, debts, and financial planning.'),
('Professional Background', 'Evaluate professional experience and current business status', 'business', 1.2, 'Assess the client''s professional background, experience, and current business or employment situation.'),
('Communication Skills', 'Assess clarity and professionalism in communication', 'personal', 1.0, 'Evaluate how clearly and professionally the client communicates.'),
('Alignment with Values', 'Check if client''s values align with company values', 'values', 1.3, 'Determine if the client''s stated values and principles align with the company''s core values.'),
('Realistic Expectations', 'Evaluate if client has realistic expectations', 'goals', 1.1, 'Assess whether the client has realistic expectations about outcomes and timelines.')
ON DUPLICATE KEY UPDATE description=VALUES(description);

-- Insert default red flags
INSERT INTO red_flags (name, description, severity, detection_keywords) VALUES
('Inconsistent Information', 'Client provides contradictory information', 'high', 'inconsistent,contradiction,changed story'),
('Financial Distress', 'Signs of severe financial problems', 'critical', 'bankruptcy,debt,foreclosure,repossession'),
('Unrealistic Expectations', 'Extremely unrealistic goals or expectations', 'medium', 'overnight success,guaranteed,get rich quick'),
('Poor Communication', 'Inability to communicate clearly or professionally', 'medium', 'rude,disrespectful,unclear'),
('Legal Issues', 'Mentions of ongoing legal problems', 'high', 'lawsuit,criminal,investigation,indicted'),
('Lack of Commitment', 'Shows minimal commitment or seriousness', 'low', 'maybe,not sure,just browsing')
ON DUPLICATE KEY UPDATE description=VALUES(description);

