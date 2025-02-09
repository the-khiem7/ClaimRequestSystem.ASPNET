-- Table: Users
CREATE TABLE Users (
    UserId SERIAL PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Role TEXT CHECK (Role IN ('Claimer', 'Approver', 'Administrator', 'Finance')) NOT NULL,
    Email VARCHAR(255) UNIQUE NOT NULL,
    Password VARCHAR(255) NOT NULL
);

-- Table: Projects
CREATE TABLE Projects (
    ProjectId SERIAL PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Description TEXT,
    AdministratorId INT,
    FOREIGN KEY (AdministratorId) REFERENCES Users(UserId)
);

-- Table: Claims
CREATE TABLE Claims (
    ClaimId SERIAL PRIMARY KEY,
    ClaimerId INT,
    ProjectId INT,
    Amount DECIMAL(10, 2) NOT NULL,
    Status TEXT CHECK (Status IN ('Submitted', 'Approved', 'Rejected', 'Returned', 'Paid', 'Canceled')) NOT NULL,
    CreatedDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (ClaimerId) REFERENCES Users(UserId),
    FOREIGN KEY (ProjectId) REFERENCES Projects(ProjectId)
);

-- Create trigger to update UpdatedDate on row update
CREATE FUNCTION update_timestamp() RETURNS TRIGGER AS $$
BEGIN
    NEW.UpdatedDate = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_update_claims_timestamp
BEFORE UPDATE ON Claims
FOR EACH ROW EXECUTE FUNCTION update_timestamp();

-- Table: ClaimActions
CREATE TABLE ClaimActions (
    ActionId SERIAL PRIMARY KEY,
    ClaimId INT,
    PerformedBy INT,
    ActionType TEXT CHECK (ActionType IN ('Create', 'Update', 'Submit', 'Cancel', 'Approve', 'Reject', 'Return', 'Paid')) NOT NULL,
    Timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    Notes TEXT,
    FOREIGN KEY (ClaimId) REFERENCES Claims(ClaimId),
    FOREIGN KEY (PerformedBy) REFERENCES Users(UserId)
);

-- Table: Staff
CREATE TABLE Staff (
    StaffId SERIAL PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Email VARCHAR(255) UNIQUE NOT NULL,
    ProjectId INT,
    FOREIGN KEY (ProjectId) REFERENCES Projects(ProjectId)
);

-- Table: Reminders
CREATE TABLE Reminders (
    ReminderId SERIAL PRIMARY KEY,
    ClaimId INT,
    Message TEXT NOT NULL,
    SentDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (ClaimId) REFERENCES Claims(ClaimId)
);
