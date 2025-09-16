-- Create EmailJobs table
CREATE TABLE IF NOT EXISTS memberorg."EmailJobs" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "CreatedBy" UUID NOT NULL REFERENCES memberorg."Users"("Id"),
    "Subject" VARCHAR(500) NOT NULL,
    "Body" TEXT NOT NULL,
    "IsHtml" BOOLEAN NOT NULL DEFAULT true,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Pending',
    "TotalRecipients" INT NOT NULL DEFAULT 0,
    "ProcessedCount" INT NOT NULL DEFAULT 0,
    "SuccessCount" INT NOT NULL DEFAULT 0,
    "FailedCount" INT NOT NULL DEFAULT 0,
    "ScheduledFor" TIMESTAMP NULL,
    "StartedAt" TIMESTAMP NULL,
    "CompletedAt" TIMESTAMP NULL,
    "ErrorMessage" TEXT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create EmailJobRecipients table
CREATE TABLE IF NOT EXISTS memberorg."EmailJobRecipients" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "JobId" UUID NOT NULL REFERENCES memberorg."EmailJobs"("Id") ON DELETE CASCADE,
    "Email" VARCHAR(255) NOT NULL,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Pending',
    "ProcessedAt" TIMESTAMP NULL,
    "ErrorMessage" TEXT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_emailjobs_status ON memberorg."EmailJobs"("Status");
CREATE INDEX IF NOT EXISTS idx_emailjobs_createdby ON memberorg."EmailJobs"("CreatedBy");
CREATE INDEX IF NOT EXISTS idx_emailjobs_schedulefor ON memberorg."EmailJobs"("ScheduledFor");
CREATE INDEX IF NOT EXISTS idx_emailjobrecipients_jobid ON memberorg."EmailJobRecipients"("JobId");
CREATE INDEX IF NOT EXISTS idx_emailjobrecipients_status ON memberorg."EmailJobRecipients"("Status");

-- Create table to track daily email quota
CREATE TABLE IF NOT EXISTS memberorg."EmailQuota" (
    "Date" DATE PRIMARY KEY,
    "EmailsSent" INT NOT NULL DEFAULT 0,
    "QuotaLimit" INT NOT NULL DEFAULT 100,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);