[TOC]



# What went good

- I did not code a single line
- the specification is very detailed, something I would not have done by myself to this amount
  - the quality is better and more detailed than a human-created specification imho

- quite swift implementation, although we lost much momentum when adding complexity
  - base app was created within a session (1 day)
  - details and quality features took much longer (~2 weeks, one session per day)

- The agent has a good idea of code structure without any rules / input
- Tests are being planned from the beginning
- The agent mode is quite autonomous, so it runs for a while without interaction

# What went bad

- annoying revisits to existing features, things that worked, stopped working
- Tests are being 'ignored' by AI and errors are not resolved, makes Tests a farce
  - not all tests go to green at first
  - then, for new features, the agent ignores the not working tests (which might be fine as they do not concern the current feature)

- complexity made it harder to get features implemented
  - more complexity, more time and effort in prompting to get a good result
  - also, the agent took several attempts to fix "obvious" errors
    - which were obvious when you see the app (which the agent does not do (yet))
    - after an unsuccessful fix, it created a resumee for the successful status, which is awkward but understandable, because it does not know better
    - this is the most annoying and tedious part at the current stage of experience imho

- code analysis feature has been 'commented out' as AI was not able to resolve the issues (yet), this is sad as I would have liked to explore this even more, great potential imho
- existing knowledge and already implemented code has not been reused but that seems to be a general challenge
  - one needs to create a central repository for reoccurring implementations like Config, Logging, Performance logs etc.

- AI is implementing things which cannot work (and it finds out after you pointing at it), on a Logical Level (e.g. trying to use PNG files as Windows Cursor without converting to CUR or ICO)
  - when such a feature does not work, and you prompt again, the agent naturally tells you it could not have worked as intended

- a human needs to be present and watch the ideas and implementations with programming knowledge
  - or at least the implementations after each spec

- you need to describe the errors precisely or submit screenshots (which is good) but AI does not get direct feedback and cannot establish a programming loop
  - Program - run - see the error - fix it
  - it tries to cope this with extensive logging, which works ok-ish, but the right level of logging could help from the start, not after issues occurred
- the code produced seems to be more (in terms of lines) than appropriate (from my experience)


# Other annotations

- at the beginning, many stops due to Kiro instabilities, has been repaired and not negative due to early access
  - Claude Sonnet 4.0 was hardly available, but there was a fallback to 3.7
  - however, this stopped at some stage, I assume Amazon did fix some volume balancing over time
  - also, over time the "budget" for AI usage got bigger, at least I was not interrupted by errors like "you used all of your AI budget, please revisit us tomorrow", which was the case in the beginning, on a daily basis
- sometimes you need to prepare prompts, which falls under prompt Engineering and is generally not enforced by the Chat. So the user gets the Impression all is well while it isn't.
- Items from the trust list have been asked for several times, list seemed to be ignored for now, which does not help in autonomy
- It is really hard to analyze and debug code you did not write and which is more complicated than necessary, potentially
- to me it seems like the agent is not applying design patterns or maintenance-friendly structure which results in tedious bug fixing
- It seems to me the agent is programming "blindly", which it does most of the time I suppose. I am not up to date on the general plans of the industry but I assume this is on the roadmap as it is a big blocker imho
- When sessions where too big, the "session summarize and create new session with old knowledge" feature did not work too good, this changed for good as well