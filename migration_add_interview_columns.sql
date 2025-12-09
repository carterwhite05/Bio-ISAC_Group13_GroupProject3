-- Migration script to add new columns for structured interview system
-- Run this if your database already exists
-- Note: If columns already exist, you'll get errors - that's okay, just ignore them

-- Add new columns to conversations table (ignore error if columns already exist)
ALTER TABLE conversations 
ADD COLUMN current_question_id INT NULL;

ALTER TABLE conversations 
ADD COLUMN waiting_for_additional_info BOOLEAN DEFAULT false;

-- Add foreign key for current_question_id (ignore error if constraint already exists)
ALTER TABLE conversations
ADD CONSTRAINT fk_conversation_current_question 
FOREIGN KEY (current_question_id) REFERENCES questions(id) ON DELETE SET NULL;

-- Create question_answers table if it doesn't exist
CREATE TABLE IF NOT EXISTS question_answers (
    id INT AUTO_INCREMENT PRIMARY KEY,
    conversation_id INT NOT NULL,
    question_id INT NOT NULL,
    answer TEXT NOT NULL,
    additional_info TEXT NULL,
    answered_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (conversation_id) REFERENCES conversations(id) ON DELETE CASCADE,
    FOREIGN KEY (question_id) REFERENCES questions(id) ON DELETE CASCADE,
    INDEX idx_conversation (conversation_id),
    INDEX idx_question (question_id)
);
