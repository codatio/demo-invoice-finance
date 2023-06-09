# Demo invoice financing

## Introduction

With our demo app, you will go through the invoice financing process flow and see how Codat makes it easier for the borrower to raise capital against the amounts due from customers, and for the lender to make an invoice financing decision.

The project is implemented in [.NET 7.0](https://dotnet.microsoft.com/en-us/download/dotnet/7.0) as a backend API that uses features of Codat's [Accounting API](https://docs.codat.io/accounting-api/overview?utm_medium=referral&utm_source=linked_website&utm_campaign=2023_github_invoice_financing_code_demo) product. You can configure and run the demo app in the terminal, or use your preferred IDE or code editor.

In the process, you will:
- Establish a connection with our test accounting platform
- Pull invoice data required for the financing assessment
- Check the invoices' eligibility based on a set of criteria we defined in the app
- Issue a decision on eligible invoices

## Prerequisites

You need these to run and test the code locally: 
- A Codat account that you can [create for free](https://signup.codat.io/?utm_medium=referral&utm_source=linked_website&utm_campaign=2023_github_invoice_financing_code_demo)
- Your Codat [API keys](https://app.codat.io/developers/api-keys?utm_medium=referral&utm_source=linked_website&utm_campaign=2023_github_invoice_financing_code_demo)
- A way to access remote systems from your locally hosted server (we used [ngrok](https://ngrok.com/))

## Getting started

To run the demo app:

1. Add your API key (`CodatApiKey`) and local machine's publicly available base url (`BaseWebhookUrl`) to the `appSettings.json` file.

3. Start your local application and use [Swagger](http://localhost:7278/swagger/index.html) to call the demo's endpoints.

5. Call `POST applications/start` to start a new invoice financing application. This returns an application id and a `linkUrl`. 

7. Provide access to your test company's demo accounting data using the `linkUrl`.

9. Call the `GET applications/{applicationId}` endpoint to check the outcome of the application.

For detailed walkthorough of prerequisites and setup steps, you can refer to our [Invoice finance build guide](https://docs.codat.io/guides/invoice-finance/setting-up).

## Details of the solution

In this demo app, we use information about a company's invoices and customers to perform risk assessment and issue an array of invoice financing decisions. In this project, we demonstrate how Codat enables you to access this data. It will help you undestand how you may implement your own automated invoice financing solution.

Our example app contains several endpoints:
* Two public endpoints allow the prospective borrower to submit a new application and retrieve the status of that application via an imaginary front end.
* Two webhook endpoints trigger the assessment process once a data connection is established and Invoice and Customer data types are fetched.

### Demo app process flow

Review the sequence diagram to visualize the steps performed by the app. We used solid arrows to depict public endpoints and dotted arrows for webhooks. 

```mermaid
  sequenceDiagram
    participant frontend as Invoice Financing Frontend 
    participant backend as Invoice Financing Backend 
    participant codat as Codat API
    frontend ->> backend: Request new application
    backend ->> codat: Create company
    codat ->> backend: New company
    backend ->> frontend: New application
    frontend ->> codat: Link accounting platform
    par
        break when status is Complete/ProcessingError
        loop
            frontend ->> backend: Get application
            backend ->> frontend: application
        end
        end
    and
        par 
            codat -->> backend: Data connection status 
        and 
            codat -->> backend: Data type sync complete
        end
        par
            backend ->> codat: Get invoices
            codat ->> backend: Invoices
        and
            backend ->> codat: Get customers
            codat ->> backend: Customers
        end
        backend ->> backend: Assess eligable invoices
        backend ->> frontend: Array of decisions per valid invoice
    end
```

### Applying for a loan

We begin when the applicant initiates a new invoice financing application by calling the `application/start` endpoint. In the background, the app creates a company using Codat's `POST /companies` endpoint, with the application Id as the company name. Codat returns the company and application Ids in the endpoint response together with a `linkUrl`. 

#### Example response returned by the `start` endpoint

```json
  {
    "id": "1c727866-6923-4f81-aa7b-c7fd8c533586",
    "codatCompanyId": "a9e28b79-6a98-4190-948d-3bd4d60e7c0a",
    "status": "Started", 
    "linkUrl": "https://link.codat.io/company/a9e28b79-6a98-4190-948d-3bd4d60e7c0a"
  }
```
Next, we need to get access to an accounting platform so we can fetch the data required to assess the risk of the loan application. Open the `linkUrl` returned in the response from POST /applications/start in your browser. Follow the flow built using [Link](https://docs.codat.io/auth-flow/authorize-embedded-link?utm_medium=referral&utm_source=linked_website&utm_campaign=2023_github_invoice_financing_code_demo), our hosted or embedded integrated authorization flow.

Select the Codat Sandbox as the source of accounting data and choose the **Invoice Financing US Company** company type. You don't need to enter any credentials to authorize this connection.

### Listening to Codat's webhooks

When the accounting platform is connected, the remaining steps will update the data requirements of the application. These are activated by Codat's webhooks that trigger specific `POST` endpoints in our example app:
* `webhooks/codat/data-connection-status` listens to the [DataConnectionStatusChanged](https://docs.codat.io/introduction/webhooks/core-rules-types#company-data-connection-status-changed?utm_medium=referral&utm_source=linked_website&utm_campaign=2023_github_invoice_financing_code_demo) webhook.

It verifies that the received data connection is an accounting platform, and assigns it to the related application via the Codat company id. 

* `webhooks/codat/datatype-sync-complete` listens to the [Data sync completed](https://docs.codat.io/introduction/webhooks/core-rules-types#data-sync-completed?utm_medium=referral&utm_source=linked_website&utm_campaign=2023_github_invoice_financing_code_demo) webhook.

It verifies the successful fetching of the `customers` and `invoices` data types from the underlying platform. In our demo, we focus on unpaid and partially paid invoices valued between 50 and 1000 USD, using the `query` parameter:

```
query = {status=submitted||status=partiallyPaid}&&currency=USD&&{amountDue>50&&amountDue<=1000}
```

From this data set, we pick up a list of unique customer Ids (`customerRef.id`) for the unpaid invoices, and then the associated customer details. Finally, we fetch all paid invoices for each of these customers to assess their previous payment behavior.

### Assessing customers and invoices

Once both data types have been filtered and fetched, they are passed to the [CustomerRiskAssessor.cs](Codat.Demos.InvoiceFinancing.Api/Services/CustomerRiskAssessor.cs) and [InvoiceFinanceAssessor.cs](Codat.Demos.InvoiceFinancing.Api/Services/InvoiceFinanceAssessor.cs) services. This is to perform risk assessment of customers and invoices, and provide a financing proposal for each eligible invoice. 

Finally, the InvoiceFinanceAssessor service returns an array of decisions to show the applicant which invoices we agree to lend against, and under what terms and conditions. The applicant can poll the `GET applications/{applicationId}` endpoint periodically to see the result once the processing is complete.

You can learn more about the decisioning logic, risk assessment criteria, and data filtering queries used by our app in our detailed [Invoice finance build guide]((https://docs.codat.io/guides/invoice-finance/inv-fin-decision).

## Next steps

🗣️ Anything unclear in this guide? Got feedback? We're working on a whole host of new content for you, so [let us know](https://github.com/orgs/codatio/discussions/new?category=general).

🔍 For detailed walkthorough of the app and its logic, refer to our [Invoice finance build guide](https://docs.codat.io/guides/invoice-finance/setting-up).
