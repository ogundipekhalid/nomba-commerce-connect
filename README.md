# nomba-commerce-connect
Layered .NET connector for Nomba's Checkout, Webhooks, Refunds and split-payment APIs, with a reference storefront — built for the DevCareer x Nomba Hackathon.
Notes from building this
Started this after getting into the Build Phase — spent most of the time getting the Nomba API flow (auth, checkout, webhooks) right since I hadn't worked with their split payment endpoint before. Still waiting on live API credentials from Nomba (asked in the Slack channel), so right now the payment calls are mocked but built against their real documented request/response shapes — swapping to live is one config flag once I get access. Next up: wrapping this as an actual OpenCart plugin so it matches a real e-commerce platform instead of just my own test storefront.
