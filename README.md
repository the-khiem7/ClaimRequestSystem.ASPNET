**FPT Software**

**Functional Specifications for**

**Claim Request**

Version: 0.9

**Ha Noi, Feb 2018**

Approval Page

| Prepared by : | Business Analyst | Signature: | ____________________ |
| --- | --- | --- | --- |
|  |  | Date: | ____ /____ / ____ |
|  |  |  |  |
| Reviewed by : | Project Manager | Signature: | ____________________ |
|  |  | Date: | ____ /____ / ____ |
|  |  |  |  |
| Supported by: | (Customer Name) | Signature: | ____________________ |
|  |  | Date: | ____ /____ / ____ |
|  |  |  |  |
|  |  |  |  |
| Approved by : | (Customer Name) | Signature: | ____________________ |
|  |  | Date: | ____ /____ / ____ |

Revision History

| Date | Version | Author | Change Description |
| --- | --- | --- | --- |
|  |  |  |  |
|  |  |  |  |
|  |  |  |  |

**Table of Contents**

[1\. Introduction 6](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625185)

[1.1 Purpose 6](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625186)

[1.2 Overview 6](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625187)

[1.3 Intended Audience and Reading Suggestions 6](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625188)

[1.4 Abbreviations 6](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625189)

[1.5 References 7](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625190)

[2\. High Level Requirements 8](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625191)

[2.1 Object Relationship Diagram 8](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625192)

[2.2 Workflow 9](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625193)

[2.3 Use Case Diagram 10](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625194)

[2.3.1 Claim Request 10](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625195)

[2.4 Permission Matrix 12](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625196)

[2.5 Site Map 13](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625197)

[2.5.1 Site Map 13](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625198)

[2.5.2 Top Navigation 13](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625199)

[3\. Use Case Specifications 14](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625200)

[3.1 Claim Request 14](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625201)

[3.1.1 UC 1: Create New Claim 14](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625202)

[3.1.2 UC 2: View Claim 18](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625203)

[3.1.3 UC 3: Update Claim 22](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625204)

[3.1.4 UC 4: Submit Claim 22](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625205)

[3.1.5 UC 5: Approve Claim 24](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625206)

[3.1.6 UC 6: Return Claim 25](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625207)

[3.1.7 UC 7: Reject Claim 26](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625208)

[3.1.8 UC 8: Paid Claim 27](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625209)

[3.1.9 UC 9: Cancel Claim 28](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625210)

[3.1.10 UC 10: Download Claim 29](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625211)

[3.2 UC 11: Manage Staff Information 31](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625212)

[3.3 UC 12: Manage Project Information 33](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625213)

[3.4 System Timer 35](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625214)

[3.4.1 UC 13: Send Email Reminder 35](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625215)

[4\. Non-Functional Requirements 36](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625216)

[5\. Other Requirements 36](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625217)

[6\. Integration 37](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625218)

[7\. Appendices 37](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625219)

[7.1 Messages List 37](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625220)

[7.2 Email Templates 39](https://www.docstomarkdown.pro/convert-word-to-markdown/#_Toc507625221)

# Introduction

## Purpose

The Functional Specification will:

*   Define the scope of business objectives, business functions, and organizational units covered,
*   Identify the business processes that the solution must facilitate,
*   Facilitate a common understanding of what the functional requirements are for all parties involved,
*   Establish a basis for defining the acceptance tests for the solution to confirm that what is delivered meets requirements.

The purpose of the document is to collect and analyse all assorted ideas that have come up to define the system, its requirements with respect to consumers. Also, we shall predict and sort out how we hope this product will be used in order to gain a better understanding of the project, outline concepts that may be developed later, and document ideas that are being considered, but may be discarded as the product develops.

## Overview

Claim Request is a centralised system that supports the creation of claims and reduces the process of paper work.

It allows all FPT Software Staffs (such as Developer, Tester, BA,…) to create and submit Claim Request for approval. PM, BUL and Finance will be approvers in the system.

## Intended Audience and Reading Suggestions

This document is intended for:

*   Development team: Responsible for developing detailed designs and implementing unit test, integration test and system test for the migrated application.
*   Documentation Team: Responsible for writing User Guide for the application.
*   UAT team: Responsible for conducting user acceptance test sessions with end users.

## Abbreviations

| Acronym | Reference |
| --- | --- |
| BR | Business Rule |
| SRS | System Requirements Specification |
| UAT | User Acceptation Test |
| UC | Use Case |

## References

N/A

# High Level Requirements

This section describes the general overview of the system functions or business processes which are depicted in different diagrams. It shows the types of users, their granted permissions to perform specific system functions and the sequence required to complete a business workflow (if any). As the section name implies, it is high-level which means may not contain detailed information. For detailed requirement specification, please refer to section **3\. Use Case Specifications**.

## Object Relationship Diagram

This section shows the static relationship between each object in the system. An object could be described as an instance of a particular entity in the system.

Figure 1: Object Relationship Diagram

**Object Description:**

| # | Object | Description |
| --- | --- | --- |
| Object |
| 1 | Claim Request | Claim Request is used to claim working payment by FSOFT Staff who works overtime |
| 2 | Staff Information | Staff Information is used to store information of staff such as: Staff Name, Rank, Salary,… |
| 3 | Project Information | Project Information is used to store information of project such as: Project Name, Start Date, End Date, Budget, …. |
| Actor |
| 1 | Claimer | All FSOFT Staffs involved in any project are claimers.This actor is able to create new, view, update, delete, and submit Claim Request for approval. |
| 2 | Approver | PM, BUL, Finance are approver of pending requests.This actor is able to update items pending for his approval and select to approve or return for amendment. |
| 3 | Finance | This actor is the final approver in approval workflow. Finance is able to update items and select to receive, process or return Claim. |
| 4 | Administrator | This actor is able to manage Staff Information, Project Information.He acts as the administrator of the application and has full rights to the application. |

## Workflow

This section shows the flow of tasks or steps taken by each user of the system in-order to complete a business process. The user’s actions are shown in each business process stage of the system and what happens before it can move to the next stage or revert to the previous.

Figure 2: Workflow Diagram

## Use Case Diagram

### Claim Request

The use case diagram here shows the specific goal and objective or how the user interacts with the system. The eclipse in the system boundary represents the system use case/functions while the stickman represents the actor/user of the system. The line connecting the actor and the use case shows that the actor can perform that function in the system to achieve a goal.

Figure 4: Use Cases

| # | UC Name | Priority Level | Description |
| --- | --- | --- | --- |
| 1 | Create New Claim Request | High | This use case allows user to create new Claim Request and save it as draft. The status of Claim Request will be “Draft”. |
| 2 | View Claim Request | Medium | This use case allows user to view Claim Request. |
| 3 | Update Claim Request | Medium | This use case allows user to update Claim Request details. |
| 4 | Submit Claim Request | High | This use case allows user to submit Claim. The status of the item will be changed to “Pending Approval”.If the Creator of Claim is the same as first Approver, system will automatically move to the next approver.An email notification will be sent to the appropriate Approver. |
| 5 | Approve Claim Request | High | This use case allows user to approve Claim.The status of Claim will be changed to “Approved”. |
| 6 | Paid Claim Request | High | This use case allow Finance staff to paid Claim Request.The status of Claim will be changed to “Paid”. |
| 7 | Reject Claim Request | Medium | This use case allows user to reject Claim Request.An email notification will be sent to the Creator of Claim.The status of Claim will be changed to “Rejected”. |
| 8 | Return Claim Request | Medium | This use case allows user to return Claim Request.An email notification will be sent to the Creator of Claim.The status of Claim will be changed to “Draft”. |
| 9 | Cancel Claim Request | Medium | This use case allows user to cancel Claim Request.The status of Claim will be changed to “Cancelled”. |
| 10 | Download Claim Request | Medium | This use case allows user to print Claim. |
| 11 | Manage Staff Information | Low | This use case allows user to manage the list of Staff Information, including creating new, viewing and editing. |
| 12 | Manage Project Information | Low | This use case allows user to manage the list of Project Information, including creating new, viewing and editing. |
| 13 | Send Email Reminder | High | This use case allows system timer to send email reminder to approvers to remind them about giving action on Claim after a specific number of days. |

## Permission Matrix

Permission Matrix mapping functions and user roles for Claims application is described as below:

Remark:

*   “X” means that user has permission on corresponding function.
*   “X\*” means that user can only work on the record that is created by him or assigned to him for approval.
*   “X\*\*” means that user can only work on the record that is pending his action.

|  | Claimer | Approver | Finance | Administrator | System Timer |
| --- | --- | --- | --- | --- | --- |
| Create New Claim Request | X |  |  |  |  |
| Status is “Draft” |
| View Claim Request | X* |  |  | X |  |
| Update Claim Request | X* |  |  |  |  |
| Delete Claim Request | X* |  |  |  |  |
| Submit Claim Request | X* |  |  |  |  |
| Status is “Pending Approval” |
| View Claim Request | X* | X* |  | X |  |
| Approve Claim Request |  | X** |  |  |  |
| Reject Claim Request |  | X** |  |  |  |
| Return Claim Request |  | X** |  |  |  |
| Status is “Cancelled” OR “Rejected” |
| View Claim | X* |  |  | X |  |
| Status is “Approved” |
| View Claim | X* | X* | X | X |  |
| Paid Claim |  |  | X |  |  |
| Print Claim |  |  | X |  |  |
| Status is “Paid” |
| View Claim | X* |  | X | X |  |
| Print Claim |  |  | X |  |  |
| Other Use Cases |
| Manage Staff Information |  |  |  | X |  |
| Manage Project Information |  |  |  | X |  |
| Send Email Reminder |  |  |  |  | X |

## Site Map

### Site Map

The site map describes the way for navigating through the system.

### Top Navigation

| # | Hyperlink | Description |
| --- | --- | --- |
| 1 | Create Claims | Click on the link to open create new Claim Request page |
| 2 | My Claims | Click on the link to show all its sub-menus.The link and all links under this section are visible for users in “Claimer” group |
| Draft | Show Claims which have status= "Draft" and Creator is the current user. |
| Pending Approval | Show Claims which have status= "Pending Approval" and Creator is the current user. |
| Approved | Show Claims which have status= "Approved" and Creator is the current user. |
| Paid | Show Claims which have status= "Paid" and Creator is the current user. |
| Rejected or Cancelled | Show Claims which have status= "Rejected" or “Cancelled” and Creator is the current user. |
| 3 | Claims for Approval | Click on the link to show all its sub-menus.The link and all links under this section are visible for users in “Approver” group. |
| For My Vetting | Show Claims which have status= "Pending Approval" and pending approval from current user. |
| Approved or Paid | Show all Claims which have status= "Approved” or “Paid” and approved by current user |
| 4 | Claims for Finance | Click on the link to show all its sub-menus.The link and all links under this section are visible for users in “Finance” group. |
| Approved | Show all Claims which have status= "Approved". |
| Paid | Show all Claims which have status= "Paid". |
| 5 | Configuration | Click on the link to show all its sub-menusThe link and all links under this section are visible for users in “Administrator” group. |
| Staff Information | Show all Staff Information in the application. |
| Project Information | Show all Project Information in the application. |

# Use Case Specifications

This section covers the system’s functional requirements which details what the system must do in term of input, behavior and the expected output. It elicits the interaction between the actor(s) and the system, the system’s behavior and the results of their interactions.

## Claim Request

### UC 1: Create New Claim

| Objective: | This use case allows user to create a new Claim Request then save as Draft. |
| --- | --- |
| Actor: | Claimer |
| Trigger: | User selects to create new Claim Request |
| Pre-conditions: | User is logged in successfully as actor above. |
| Post-condition: | A new Claim Request is created. |

**Screen Navigation:**

User click on “**Create Claims**” in the top navigation

| Component | Data Type | Editable | Mandatory | Description |
| --- | --- | --- | --- | --- |
| Top right corner text | Single line of text | No | No | Display text: “Claim Status: <<Claim Status>>”>. |
| Staff Name | Person or Group | No | No | Display name of user who creates the Claim. |
| Staff Department | Single line of text | N.A | N.A | Display department of current user |
| Staff ID | Single line of text | No | No | Display Staff ID of user who creates the Claim |
| Project Name | Dropdown list | Yes | Yes | List of all Project Information, display Project Name in dropdown.Sort value in alphabet |
| Role in Project | Single line of text | No | No | Show role of user in selected project |
| Project Duration | Single line of text | No | No | Show project duration of selected project |
| Claim Table section |
| Date | Date | Yes | Yes | User selects date to claim.Value range of date is from “Start Date” and “End Date” of “Start Date – End Date”. |
| Day | Single line of text | No | No | Based on the selected date, display day in the week in this field.Format: DDDE.g.: Mon, Tue |
| From | Date and Time | Yes | Yes | User selects start time of actual working hours. |
| To | Date and Time | Yes | Yes | User selects end time of actual working hours. |
| Total No. of Hours | Number | No | Yes | User input total working hour |
| Remarks | Single line of text | Yes | No | Creator inputs remarks for each record. |
| Total working hour | Number | No | No | Get total working hour of the table |
| Add more | Button | N.A | N.A | User clicks on this button to add new row in Claim table. |
| Remarks | Multiple lines of text | Yes | No | User input remarks for this Claim. |
| Audit Trail | Multiple lines of text | No | No | System will automatically record changes made by user. |

**Visible Button Table**

| Button | Description |
| --- | --- |
| Save | This button is shown in New Mode or Edit Mode when user save changes on an existing Claim.User clicks on this button to save all the changes. |
| Submit | User clicks on this button to submit Claim. |
| Approve | User clicks on this button to approve Claim. |
| Reject | User clicks on this button to reject Claim. |
| Return | User clicks on this button to Return Claim. |
| Print | User clicks on this button to print out Claim info as in the site screen. |
| Cancel Request | User clicks on this button to Cancel Claim. |
| Cancel | This button is shown in New Mode or Edit Mode when user updates an existing Claim.User clicks on this button to cancel updating then back to view. |
| Close | This button is shown in Display ModeUser clicks on this button to back to view |

3: Select a Run or Course- Popup screen when Course Type is from ACCE

**Activities Flows**

**Business Rules**

| Step | BR Code | Description |
| --- | --- | --- |
|  | BR 1 | Saving Rules:System will perform the following actions:Save all changesUpdates the status of the Claim to “Draft”.Append a new line into “Audit Trail”: “Created on <<Current date time>> by <<current user>>.After saving successfully, system stays in the current screen (the edit mode of the current Claim). |

### UC 2: View Claim

| Objective: | This use case allows user to view details of a Claim. |
| --- | --- |
| Actor: | Claimer, Approver, Finance and Administrator |
| Trigger: | User selects to open an existing Claim. |
| Pre-conditions: | User is logged in successfully as actor above. |
| Post-condition: | A Claim is opened for viewing. |

**Screen Navigation:**

*   User accesses **My Claims > Draft.**
*   System displays items which have status= "Draft" and Creator is the current user. Data is retrieved from Claim Request.

**Screen 5: Draft Claim Request**

| Column Name | Value | Description |
| --- | --- | --- |
| Claim ID | [Claim ID] | User can sort/ filter in this column.Clicks to the link to open the detail page.Sorts ascending by default. |
| Staff Name | Staff Name | User can sort/ filter in this column. |
| Project Name | Project Name | User can sort/ filter in this column. |
| Project Duration | From – To | User can sort/ filter in this column. |
| Total working hour | Total working hour | User can sort/ filter in this column. |

*   User accesses **My Claims > Pending approval.**
*   System displays items which have status= "Pending approval" and Creator is the current user.
*   Data is retrieved from Claim Request.

| Column Name | Value | Description |
| --- | --- | --- |
| Claim ID | [Claim ID] | User can sort/ filter in this column.Clicks to the link to open the detail page.Sorts ascending by default. |
| Staff Name | Staff Name | User can sort/ filter in this column. |
| Project Name | Project Name | User can sort/ filter in this column. |
| Project Duration | From – To | User can sort/ filter in this column. |
| Total working hour | Total working hour | User can sort/ filter in this column. |

*   User accesses **My Claims > Approved.**
*   System displays items which have status= "Approved" and Creator is the current user.
*   Data is retrieved from Claim Request.

| Column Name | Value | Description |
| --- | --- | --- |
| Claim ID | [Claim ID] | User can sort/ filter in this column.Clicks to the link to open the detail page.Sorts ascending by default. |
| Staff Name | Staff Name | User can sort/ filter in this column. |
| Project Name | Project Name | User can sort/ filter in this column. |
| Project Duration | From – To | User can sort/ filter in this column. |
| Total working hour | Total working hour | User can sort/ filter in this column. |

*   User accesses **My Claims > Paid.**
*   System displays items which have status= "Paid" and Creator is the current user.
*   Data is retrieved from Claim Request.

| Column Name | Value | Description |
| --- | --- | --- |
| Claim ID | [Claim ID] | User can sort/ filter in this column.Clicks to the link to open the detail page.Sorts ascending by default. |
| Staff Name | Staff Name | User can sort/ filter in this column. |
| Project Name | Project Name | User can sort/ filter in this column. |
| Project Duration | From – To | User can sort/ filter in this column. |
| Total working hour | Total working hour | User can sort/ filter in this column. |

*   User accesses **My Claims > Rejected or Cancelled.**
*   System displays items which have status= "Rejected" or “Cancelled” and Creator is the current user.
*   Data is retrieved from Claim Request.

| Column Name | Value | Description |
| --- | --- | --- |
| Claim ID | [Claim ID] | User can sort/ filter in this column.Clicks to the link to open the detail page.Sorts ascending by default. |
| Staff Name | Staff Name | User can sort/ filter in this column. |
| Project Name | Project Name | User can sort/ filter in this column. |
| Project Duration | From – To | User can sort/ filter in this column. |
| Total working hour | Total working hour | User can sort/ filter in this column. |

*   User accesses **Claim for Approval > For my Vetting.**
*   System displays items which have status= "Pending Approval".
*   Data is retrieved from Claim Request.

| Column Name | Value | Description |
| --- | --- | --- |
| Claim ID | [Claim ID] | User can sort/ filter in this column.Clicks to the link to open the detail page.Sorts ascending by default. |
| Staff Name | Staff Name | Group by this column |
| Project Name | Project Name | Group by this column |
| Project Duration | From – To | User can sort/ filter in this column. |
| Total working hour | Total working hour | User can sort/ filter in this column. |
| Total Claim Amount | Total Claim Amount | User can sort/ filter in this column. |

*   User accesses **Claim for Approval > Approved or Paid.**
*   System displays items which have status= "Approved" or “Paid”.
*   Data is retrieved from Claim Request.

| Column Name | Value | Description |
| --- | --- | --- |
| Claim ID | [Claim ID] | User can sort/ filter in this column.Clicks to the link to open the detail page.Sorts ascending by default. |
| Staff Name | Staff Name | Group by this column |
| Project Name | Project Name | Group by this column |
| Project Duration | From – To | User can sort/ filter in this column. |
| Total working hour | Total working hour | User can sort/ filter in this column. |
| Total Claim Amount | Total Claim Amount | User can sort/ filter in this column. |

*   User accesses **Claim for Approval > Approved.**
*   System displays items which have status= "Approved"
*   Data is retrieved from Claim Request.

| Column Name | Value | Description |
| --- | --- | --- |
| Claim ID | [Claim ID] | User can sort/ filter in this column.Clicks to the link to open the detail page.Sorts ascending by default. |
| Staff Name | Staff Name | Group by this column |
| Project Name | Project Name | Group by this column |
| Project Duration | From – To | User can sort/ filter in this column. |
| Total working hour | Total working hour | User can sort/ filter in this column. |
| Total Claim Amount | Total Claim Amount | User can sort/ filter in this column. |

*   User accesses **Claim for Approval > Paid.**
*   System displays items which have status= “Paid”.
*   Data is retrieved from Claim Request.

| Column Name | Value | Description |
| --- | --- | --- |
| Claim ID | [Claim ID] | User can sort/ filter in this column.Clicks to the link to open the detail page.Sorts ascending by default. |
| Staff Name | Staff Name | Group by this column |
| Project Name | Project Name | Group by this column |
| Project Duration | From – To | User can sort/ filter in this column. |
| Total working hour | Total working hour | User can sort/ filter in this column. |
| Total Claim Amount | Total Claim Amount | User can sort/ filter in this column. |
| Download Claims | Button | Click button to download Claims |

### UC 3: Update Claim

| Objective: | This use case allows user to update an existing Claim Request |
| --- | --- |
| Actor: | Claimer |
| Trigger: | User selects to update an existing Claim. |
| Pre-conditions: | User is logged in successfully as actor above |
| Post-condition: | A Claim Request is updated. |

**Screen Navigation:**

User clicks on detailed view of a Claim and input value.

**Activities Flows**

**Business Rules**

| Step | BR Code | Description |
| --- | --- | --- |
| (4) | BR 3 | Updating Rules:System will perform the following actions:Save all changesAppend a new line into “Audit Trail”: “Updated on <<Current date time>> by <<current user>>.After saving successfully, system stays in the current screen (the edit mode of the current Claim). |

### UC 4: Submit Claim

| Objective: | This use case allows user to submit a Claim. |
| --- | --- |
| Actor: | Claimer |
| Trigger: | User select to submit a Claim. |
| Pre-conditions: | User is logged in successfully as actor above.“Claim Status” must be “Draft” |
| Post-condition: | A Claim is submitted. |

**Screen Navigation:**

In **Screen 2: Claim Request**, user clicks on “**Submit**” button. A confirmation message will show up. User clicks “**OK**” to submit Claim.

**Activities Flows**

**Business Rules**

| Step | BR Code | Description |
| --- | --- | --- |
| (2) | BR 6 | Validating Rules:If any mandatory field in the Claim form is left blank, system will show an error message for the required fields MSG 7(Refer to Messages List)If user select 2 or more rows with the same day, system will show an error message MSG 4 |
| (3) | BR 7 | Confirmation Message Displaying Rules:System will show the confirmation message MSG 6(Refer to Messages List).If user clicks on “OK” button, system will proceed “Submitting Rules” below.Otherwise, if user clicks on “Cancel”, system will close the dialog and back to the current screen. |
| (3) | BR 8 | Submitting Rules:System will perform the following actions:System query into Project Information list to select PM of the project then send notification email using ET 1(Refer to Email Templates).Update “Claim Status” to “Pending Approval”.Update “Submitted Date” to the current date.Append new line into Audit Trail: “Submitted by <<current user name>> on <<current date time>>. |

### UC 5: Approve Claim

| Objective: | This use case allows user to approve Claim. |
| --- | --- |
| Actor: | Approver |
| Trigger: | User selects to approve Claim. |
| Pre-conditions: | User is logged in successfully as actor above.“Claim Status” must be “Pending Approval” |
| Post-condition: | Claim is approved. |

**Screen Navigation:**

In **Screen 2: Claim Form**, user selects records in Claim Table and clicks on “**Approve**” button. A confirmation message will show up. User clicks “**OK**” to approve Claim.

Alternatively, in **Screen 11**, user selects multiple items and clicks on “**Approve**” button. A confirmation message will show up. User clicks “**OK**” to approve Claim.

**Activities Flows**

**Business Rules**

| Step | BR Code | Description |
| --- | --- | --- |
| (3) | BR 9 | Confirmation Message Displaying Rules:System will show the confirmation message MSG 8(Refer to Messages List).If user clicks on “OK” button, system will proceed “Approving Rules” below.Otherwise, if user clicks on “Cancel”, system will close the dialog and back to the current screen. |
| (3) | BR 10 | Approving Rules:System will send notification email to group mail of Finance using ET 2 and creator using ET 3 (Refer to Email Templates).Update “Claim Status” to “Approved”.Update “Approved Date” to the current date.Append new line into Audit Trail: “Approved by <<current user name>> on <<current date time>>. |

### UC 6: Return Claim

| Objective: | This use case allows user to return Claim. |
| --- | --- |
| Actor: | Approver |
| Trigger: | User selects to return Claim. |
| Pre-conditions: | User is logged in successfully as actor above.“Claim Status” must be “Pending Approval” for Approver to return Claim. |
| Post-condition: | Claim is returned for amendments. |

**Screen Navigation:**

In **Screen 2: Claim Form**, user clicks on “**Return**” button. A confirmation message will show up. User clicks “**OK**” to return Claim.

**Activities Flows**

**Business Rules**

| Step | BR Code | Description |
| --- | --- | --- |
| (3) | BR 11 | Confirmation Message Displaying Rules:If current user clicks on “Return” button without inputting value in “Remarks”, system will show an error message MSG 12(Refer to Messages List).System will show the confirmation message MSG 10(Refer to Messages List).If user clicks on “OK” button, system will proceed “Returning Rules” below.Otherwise, if user clicks on “Cancel”, system will close the dialog and back to the current screen. |
| (3) | BR 12 | Returning Rules:Update “Claim Status” to “Draft”.Append new line into Audit Trail: “Returned by <<approver name>> on <<current date time>>.System sends a notification email to creator of claim, using ET 4(Refer to Email Templates).After returning in Screen 2: Claim Form, system redirects to the previous view. |

### UC 7: Reject Claim

| Objective: | This use case allows user to Reject Claim. |
| --- | --- |
| Actor: | Approver |
| Trigger: | User selects to void Claim. |
| Pre-conditions: | User is logged in successfully as actor above.“Claim Status” must be “Rejected”. |
| Post-condition: | Claim is rejected. |

**Screen Navigation:**

In **Screen 2: Claim Form**, user clicks on “**Reject**” button. A confirmation message will show up. User clicks “**OK**” to void Claim.

**Activities Flows**

**Business Rules**

| Step | BR Code | Description |
| --- | --- | --- |
| (3) | BR 13 | Confirmation Message Displaying Rules:System will show the confirmation message 107 (Refer to Messages List).If user clicks on “OK” button, system will proceed rule below.Otherwise, if user clicks on “Cancel”, system will close the dialog and back to the current screen. |
| (3) | BR 14 | Void Rules:Update “Claim Status” to “Rejected”.Append new line into Audit Trail: “Rejected by <<Approver Name>> on <<current date time>>.After returning in Screen 2: Claim Form, system redirects to the previous view. |

### UC 8: Paid Claim

| Objective: | This use case allows user to Paid Claim. |
| --- | --- |
| Actor: | Finance |
| Trigger: | User selects to receive Claim. |
| Pre-conditions: | User is logged in successfully as actor above.“Claim Status” must be “Approved”. |
| Post-condition: | A Claim is paid by Finance. |

**Screen Navigation:**

In **Screen 2: Claim Form**, user clicks on “**Paid**” button. A confirmation message will show up. User clicks “**OK**” to receive Claim.

Alternatively, in **Screen 14,** user selects multiple items and clicks on “**Paid**” button. A confirmation message will show up. User clicks “**OK**” to receive Claim.

**Activities Flows**

**Business Rules**

| Step | BR Code | Description |
| --- | --- | --- |
| (3) | BR 15 | Confirmation Message Displaying Rules:System will show the confirmation message MSG 13(Refer to Messages List).If user clicks on “OK” button, system will proceed rule below.Otherwise, if user clicks on “Cancel”, system will close the dialog and back to the current screen. |
| (3) | BR 16 | Receiving Rules:Update “Claim Status” to “Paid”.Append new line into Audit Trail: “Paid by <<approver name>> on <<current date time>>.After returning in Screen 2: Claim Form, system redirects to the previous view. |

### UC 9: Cancel Claim

| Objective: | This use case allows user to cancel Claim. |
| --- | --- |
| Actor: | Creator |
| Trigger: | User selects to cancel Claim. |
| Pre-conditions: | User is logged in successfully as actor above.“Claim Status” must be “Draft”. |
| Post-condition: | A Claim is cancelled by Creator. |

**Screen Navigation:**

In **Screen 2: Claim Form**, user clicks on “**Cancel Claim**” button. A confirmation message will show up. User clicks “**OK**” to receive Claim.

**Activities Flows**

**Business Rules**

| Step | BR Code | Description |
| --- | --- | --- |
| (3) | BR 17 | Confirmation Message Displaying Rules:System will show the confirmation message MSG 14(Refer to Messages List).If user clicks on “OK” button, system will proceed rule below.Otherwise, if user clicks on “Cancel”, system will close the dialog and back to the current screen. |
| (3) | BR 18 | Receiving Rules:Update “Claim Status” to “Cancelled”.Append new line into Audit Trail: “Cancelled by <<creator name>> on <<current date time>>.After returning in Screen 2: Claim Form, system redirects to the previous view. |

### UC 10: Download Claim

| Objective: | This use case allows user to download Claim. |
| --- | --- |
| User: | Finance |
| Trigger: | User selects to download Claim. |
| Pre-conditions: | User is logged in successfully as user above.“Claim Status” must be “Paid” |
| Post-condition: | A Claim is/are downloaded. |

**Screen Navigation**

In the view **Claim for Finance > Paid,** user selects multiple items and clicks on “**Download Claim**” button

**Activities Flows**

N.A

**Business Rules**

| Step | BR Code | Description |
| --- | --- | --- |
|  | BR 19 | Download Rules:System query all Claim Request have Paid in the current monthSystem generate excel file then allow user to downloadThe template of excel file as |

## UC 11: Manage Staff Information

| Objective: | This use case allows user to create/view/update Staff Information |
| --- | --- |
| Actor: | Administrator |
| Trigger: | User selects to create/view/update Staff Information |
| Pre-conditions: | User is logged in successfully as actor above. |
| Post-condition: | A Staff Information is created/viewed/updated. |

**Screen Navigation:**

User expands “**Configuration**” in the top navigation panel and clicks on “**Staff Information**”.

| Component | Data Type | Editable | Mandatory | Description |
| --- | --- | --- | --- | --- |
| Staff Name | User field | Yes | Yes | To allow select user |
| Department | Single line of text | Yes | Yes |  |
| Job Rank | Single line of text | Yes | Yes |  |
| Salary | Number field | Yes | Yes |  |
| Save | Button | N.A | N.A | This button is shown in New Mode and Edit Mode when user creates new item / updates an existing item.User clicks on this button to save all changes. |
| Cancel/Close | Button | N.A | N.A | User clicks on this button to discard changes and navigate back to previous view. |
| Edit | Button | N.A | N.A | This button is shown in Read Mode, user clicks on this button to switch to Edit Mode. |

**Activities Flows**

Create/View/Update/Delete

**Business Rules**

| Step | BR Code | Description |
| --- | --- | --- |
| (4) | BR 39 | Creating Rules:If any of required fields is left blank, system will show an error message for the required fields MSG 7(Refer to Messages List)After passing validation, system will:Create new item with input data.Navigate user back to the previous view. |
| (4) | BR 40 | Viewing Rules:View item in Display form |
| (4) | BR 41 | Updating Rules:If any of required fields is left blank, system will show an error message for the required fields MSG 7(Refer to Messages List)After passing validation, system will:Update item with input data.Navigate user back to the previous view. |

## UC 12: Manage Project Information

| Objective: | This use case allows user to create/view/update Project Information |
| --- | --- |
| Actor: | Administrator |
| Trigger: | User selects to create/view/update Project Information |
| Pre-conditions: | User is logged in successfully as actor above. |
| Post-condition: | A Project Information is created/viewed/updated. |

**Screen Navigation:**

User expands “**Configuration**” in the top navigation panel and clicks on “**Project Information**”.

| Component | Data Type | Editable | Mandatory | Description |
| --- | --- | --- | --- | --- |
| Project Name | Single line of text | Yes | Yes |  |
| Project Code | Single line of text | Yes | No | Max length: 20 characters |
| Duration |  |  |  |  |
| From | Date Time | Yes | Yes | Date only |
| To | Date Time | Yes | Yes | Date only |
| PM | User or Group | Yes | Yes | Single selection, user only |
| QA | User or Group | Yes | Yes | Single selection, user only |
| Technical lead | User or Group | Yes | Yes | Multiple selection, user only |
| BA | User or Group | Yes | Yes | Multiple selection, user only |
| Developers | User or Group | Yes | Yes | Multiple selection, user only |
| Testers | User or Group | Yes | Yes | Multiple selection, user only |
| Technical Consultancy | User or Group | Yes | Yes | Multiple selection, user only |
| Save | Button | N.A | N.A | This button is shown in New Mode and Edit Mode when user creates new item / updates an existing item.User clicks on this button to save all changes. |
| Cancel/Close | Button | N.A | N.A | User clicks on this button to discard changes and navigate back to previous view. |
| Edit | Button | N.A | N.A | This button is shown in Read Mode, user clicks on this button to switch to Edit Mode. |

**Activities Flows**

Create/View/Update/Delete

**Business Rules**

| Step | BR Code | Description |
| --- | --- | --- |
| (4) | BR 42 | Creating Rules:If any of required fields is left blank, system will show an error message for the required fields MSG 7(Refer to Messages List)After passing validation, system will:Create new item with input data.Navigate user back to the previous view. |
| (4) | BR 43 | Viewing Rules:View item in Display mode |
| (4) | BR 44 | Updating Rules:If any of required fields is left blank, system will show an error message for the required fields MSG 7(Refer to Messages List)After passing validation, system will:Update item with input data.Navigate user back to the previous view. |

## System Timer

### UC13: Send Email Reminder

| Objective: | This use case allows system timer to send notification email approvers |
| --- | --- |
| Actor: | System Timer |
| Trigger: | Daily at 1:00 AM |
| Pre-conditions: | N.A |
| Post-condition: | Email reminder is sent. |

**Activity Flows**

N.A

**Business Rules**

| BR Code | Description |
| --- | --- |
| BR 47 | Email Sending Rules:System retrieves the list of Claim Request of which “Claim Status” is “Pending Approval” and the last modified date is smaller than the current date.System send email to approver using email template ET 5(Refer to Email Templates for more details).Note: each Approver will received an email once per day which contains all his pending approval Claims. |

# Non-Functional Requirements

1.  Response time for normal request is not exceed 3 seconds
2.  Response time for download request is not exceed 30 seconds
3.  System available over 99.5%
4.  System support around 5000 users, 200 concurrent users
5.  Data volume around 12,000 records Claim Request. And grown 3,000 records each year.

# Other Requirements

1.  Report
2.  Statistic

# Integration

N/A

# Appendices

## Messages List

| # | Message Code | Message | Description |
| --- | --- | --- | --- |
|  | MSG 1 | Cannot create a new Claim Request as there is no Claim Request Configuration in the system. Please ask Administrator to create Claim Request Configuration in order to create new claims. |  |
|  | MSG 2 | This action will delete Claim permanently. Please click 'OK' to delete the claim or 'Cancel' to the close the dialog. |  |
|  | MSG 3 | Please indicate that you have read and agree to the Terms and Conditions and Privacy Policy. |  |
|  | MSG 4 | Duplicated Claim. Please update your Claim information and submit again.Claim Duplicated: <<Claim ID>> |  |
|  | MSG 5 | Please accept your Letter of Appointment in selected Run Details/Course Schedule first and submit again. |  |
|  | MSG 6 | This action will Submit Claim.Please click ‘OK’ to submit the claim or ‘Cancel’ to close the dialog. |  |
|  | MSG 7 | Please specify value for this field. |  |
|  | MSG 8 | This action will approve Claim.Please click ‘OK’ to approve the claim or ‘Cancel’ to close the dialog. |  |
|  | MSG 9 | This action will reject Claim.Please click ‘OK’ to reject the claim or ‘Cancel’ to close the dialog. |  |
|  | MSG 10 | This action will return Claim.Please click ‘OK’ to return the claim or ‘Cancel’ to close the dialog. |  |
|  | MSG 11 | This action will Reject claim.Please click ‘OK’ to return the claim or ‘Cancel’ to close the dialog. |  |
|  | MSG 12 | Please input your remarks in order to return Claim. |  |
|  | MSG 13 | This action will paid Claim.Please click ‘OK’ to receive the claim or ‘Cancel’ to close the dialog. |  |
|  | MSG 14 | This action will cancel Claim.Please click ‘OK’ to process the claim or ‘Cancel’ to close the dialog. |  |
|  | MSG 15 | Cannot create a new Generic claim as there is no Generic Claim Configuration in the system. Please ask Administrator to create Generic Claim Configuration in order to create new claims. |  |
|  | MSG 16 | Cannot submit as you do not have any developer record in the selected run.Please select another Run and submit again. |  |
|  | MSG 17 | Claim Type entered already exists. Please enter a new claim type |  |

## Email Templates

ET 1: Send email to Supervisor when Claimer submits Claim Request

| Send to | PM of project |
| --- | --- |
| CC | Claimer |
| Subject | Claim Request <<Project Name>> << Staff Name>> - <<Staff ID>> |
| Body | Dear <<PM Name>>,Claim Request for <<Project Name>> << Staff Name>> - <<Staff ID>> is submitted and waiting for approval.Please access the Claim via the following link:<<Link to item>>Sincerely,System AdministratorNote: This is an auto-generated email, please do not reply. |

ET 2: Send email to Finance group when Final Approver approves Claim Request

| Send to | Finance group |
| --- | --- |
| CC |  |
| Subject | Claim Request<<Project Name>> << Staff Name>> - <<Staff ID>> |
| Body | Dear Finance team,Claim Request for <<Project Name>> << Staff Name>> - <<Staff ID>> is approved and pending for your action.Please access the Claim via the following link:<<Link to item>>Sincerely,System AdministratorNote: This is an auto-generated email, please do not reply. |

ET 3: Send email to creator when Approver approves Claim Request

| Send to | Creator |
| --- | --- |
| CC |  |
| Subject | Claim Request <<Project Name>> << Staff Name>> - <<Staff ID>> |
| Body | Dear <<Staff Name>>,Claim Request for <<Project Name>> << Staff Name>> - <<Staff ID>> is Approved by approver.Please access the Claim via the following link:<<Link to item>>Sincerely,System AdministratorNote: This is an auto-generated email, please do not reply. |

ET 4: Send email to creator when Approver/Finance returns Claim Request

| Send to | Creator |
| --- | --- |
| CC |  |
| Subject | Claim Request <<Project Name>> << Staff Name>> - <<Staff ID>> |
| Body | Dear <<Staff Name>>,Claim Request for <<Project Name>> << Staff Name>> - <<Staff ID>> is returned.Please access the Claim via the following link:<<Link to item>>Sincerely,System AdministratorNote: This is an auto-generated email, please do not reply. |

ET 5: Send email to Approver for the list of pending approval Claims

| Send to | Approver |
| --- | --- |
| CC | N.A |
| Subject | Pending Approval Claims |
| Body | Dear < Approver Name >,There is/are Claim(s) for the Staff below that has been pending for your approval:<<Project Name 1>> << Staff Name 1>> - <<Staff ID 1>><< Project Name 2>> << Staff Name 2>> - <<Staff ID 2>><< Project Name 3>> << Staff Name 1>> - <<Staff ID 2>>…Please click on the following link to view your pending approval Claim :{Link to the view item 1}{Link to the view item 2}....Sincerely,System AdministratorNote: This is an auto-generated email, please do not reply. |
