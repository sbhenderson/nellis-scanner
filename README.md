# Nellis Scanner

Scan Nellis Auctions to determine the quality of the marketplace. We will:

1. Grab the first X listings on some frequency
2. Keep track of future listings for a given inventory ID
3. Store data in a PostgreSQL instance
4. Analyze the data and draw some conclusions

## Background

I pass by the Nellis Auctions building on 99 in the Katy area often, and on weekends, I see a **lot** of people picking up their orders. Far more than I would have expected although when you look at the number of items in the inventory, it becomes somewhat obvious why this could be the case. So I created an account and just watched a few auctions. These are confusing (neither good nor bad) signs:

1. Website does not list what closing prices were even if you had the link to the item.
2. A few items that supposedly closed "came back" a day or two later. Were there that many returns? Are there hidden reserve prices?
3. The 15% buyer's premium is not added to the displayed cost which gives you a false sense of a "deal" when comparing with other retailers.

The only way to draw conclusions is to have data, and that's the motivation for this project.

## Philosophy

This is my personal test project to go full vibe coding. I am going to use agentic AI via GitHub Copilot with Claude 3.7 as much as possible. I will list areas where I struggled with.

## References

1. This [repository](https://github.com/Brudderbot/nellisAuction) revealed the hidden query parameter that returns data in JSON instead of HTML: `&_data=routes%2Fsearch`. I will probably be taking a look at the cookie usage as I see how important that is for the API.

## Notice

Relating to Nellis Auction, the website/company/brand, this project reserves no rights relating to it and any content downloaded through the course of this work. Their [terms](https://www.nellisauction.com/terms).
