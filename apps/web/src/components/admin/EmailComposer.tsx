import React, { useState, useRef } from 'react';
import api from '../../services/api';
import './EmailComposer.css';

interface EmailComposerProps {
  className?: string;
}

const EmailComposer: React.FC<EmailComposerProps> = ({ className = '' }) => {
  const [toEmails, setToEmails] = useState('');
  const [subject, setSubject] = useState('');
  const [body, setBody] = useState('');
  const [isHtml, setIsHtml] = useState(true);
  const [sending, setSending] = useState(false);
  const [message, setMessage] = useState<{ type: 'success' | 'error', text: string } | null>(null);
  const editorRef = useRef<HTMLDivElement>(null);

  const formatText = (command: string, value?: string) => {
    if (!isHtml) return;
    document.execCommand(command, false, value);
    editorRef.current?.focus();
  };

  const insertList = (type: 'ul' | 'ol') => {
    if (!isHtml) return;
    document.execCommand(type === 'ul' ? 'insertUnorderedList' : 'insertOrderedList', false);
    editorRef.current?.focus();
  };

  const insertLink = () => {
    if (!isHtml) return;
    const url = prompt('Enter URL:');
    if (url) {
      document.execCommand('createLink', false, url);
      editorRef.current?.focus();
    }
  };

  const handleEditorChange = () => {
    if (editorRef.current) {
      setBody(editorRef.current.innerHTML);
    }
  };

  const parseEmails = (emailString: string): string[] => {
    if (!emailString.trim()) return [];
    
    return emailString
      .split(',')
      .map(email => email.trim())
      .filter(email => email.length > 0);
  };

  const validateEmail = (email: string): boolean => {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  };

  const handleSendEmail = async () => {
    const toEmailList = parseEmails(toEmails);

    if (toEmailList.length === 0) {
      setMessage({ type: 'error', text: 'Please enter at least one recipient email address' });
      return;
    }

    // Validate all email addresses
    const invalidEmails = toEmailList.filter(email => !validateEmail(email));
    
    if (invalidEmails.length > 0) {
      setMessage({ 
        type: 'error', 
        text: `Invalid email address(es): ${invalidEmails.join(', ')}` 
      });
      return;
    }

    if (!subject.trim()) {
      setMessage({ type: 'error', text: 'Please enter a subject' });
      return;
    }

    if (!body.trim()) {
      setMessage({ type: 'error', text: 'Please enter email content' });
      return;
    }

    setSending(true);
    setMessage(null);

    try {
      const response = await api.post('/adminemail/send', {
        toEmails: toEmailList,
        subject,
        body,
        isHtml
      });

      if (response.data.success) {
        setMessage({ type: 'success', text: response.data.message });
        // Clear form
        setToEmails('');
        setSubject('');
        setBody('');
        if (editorRef.current) {
          editorRef.current.innerHTML = '';
        }
      } else {
        setMessage({ type: 'error', text: response.data.message || 'Failed to send email' });
      }
    } catch (error: any) {
      setMessage({ 
        type: 'error', 
        text: error.response?.data?.message || 'Failed to send email. Please try again.' 
      });
    } finally {
      setSending(false);
    }
  };

  return (
    <div className={`email-composer-container ${className}`}>
      <div className="email-composer-card">
        <div className="email-composer-header">
          <div className="email-composer-title">
            <svg className="email-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
            </svg>
            <h2>Compose Email</h2>
          </div>
        </div>

        {message && (
          <div className={`email-alert email-alert-${message.type}`}>
            <svg className="alert-icon" fill="currentColor" viewBox="0 0 20 20">
              {message.type === 'success' ? (
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
              ) : (
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
              )}
            </svg>
            <p>{message.text}</p>
          </div>
        )}

        <div className="email-composer-body">
          <div className="email-field-group">
            <label htmlFor="to-emails" className="email-label">Recipients:</label>
            <input
              type="text"
              id="to-emails"
              value={toEmails}
              onChange={(e) => setToEmails(e.target.value)}
              placeholder="Enter email addresses separated by commas (e.g., user1@example.com, user2@example.com)"
              className="email-input"
            />
            <small className="email-help-text">Each recipient will receive an individual email (privacy protected)</small>
          </div>

          <div className="email-field-group">
            <label htmlFor="subject" className="email-label">Subject:</label>
            <input
              type="text"
              id="subject"
              value={subject}
              onChange={(e) => setSubject(e.target.value)}
              placeholder="Enter email subject"
              className="email-input"
            />
          </div>

          <div className="email-field-group">
            <div className="email-field-header">
              <label className="email-label">Message:</label>
              <label className="email-format-toggle">
                <input
                  type="checkbox"
                  checked={isHtml}
                  onChange={(e) => setIsHtml(e.target.checked)}
                />
                <span>HTML Format</span>
              </label>
            </div>
            
            {isHtml && (
              <div className="email-toolbar">
                <button onClick={() => formatText('bold')} className="toolbar-btn" title="Bold">
                  <strong>B</strong>
                </button>
                <button onClick={() => formatText('italic')} className="toolbar-btn" title="Italic">
                  <em>I</em>
                </button>
                <button onClick={() => formatText('underline')} className="toolbar-btn" title="Underline">
                  <u>U</u>
                </button>
                <div className="toolbar-separator"></div>
                <button onClick={() => insertList('ul')} className="toolbar-btn" title="Bullet List">
                  • List
                </button>
                <button onClick={() => insertList('ol')} className="toolbar-btn" title="Numbered List">
                  1. List
                </button>
                <div className="toolbar-separator"></div>
                <button onClick={insertLink} className="toolbar-btn" title="Insert Link">
                  🔗 Link
                </button>
              </div>
            )}
            
            {isHtml ? (
              <div
                ref={editorRef}
                contentEditable
                onInput={handleEditorChange}
                className="email-editor"
              />
            ) : (
              <textarea
                value={body}
                onChange={(e) => setBody(e.target.value)}
                rows={12}
                placeholder="Enter your message here..."
                className="email-textarea"
              />
            )}
          </div>
        </div>

        <div className="email-composer-footer">
          <div className="email-footer-info">
            Emails will be sent individually to each recipient with BCFR template styling
          </div>
          <button
            onClick={handleSendEmail}
            disabled={sending}
            className="email-send-btn"
          >
            {sending ? (
              <>
                <span className="spinner"></span>
                Sending...
              </>
            ) : (
              <>
                <svg className="send-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8" />
                </svg>
                Send Email
              </>
            )}
          </button>
        </div>
      </div>

      <div className="email-tips-card">
        <div className="email-tips-header">
          <svg className="tips-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <h3>Email Tips</h3>
        </div>
        <ul className="email-tips-list">
          <li>Enter multiple email addresses separated by commas</li>
          <li>Each recipient receives an individual email for privacy</li>
          <li>Recipients won't see other email addresses</li>
          <li>All emails are automatically wrapped in the BCFR template</li>
          <li>You can use HTML formatting for rich text content</li>
          <li>Test your email with a small group before sending to all members</li>
        </ul>
      </div>
    </div>
  );
};

export default EmailComposer;