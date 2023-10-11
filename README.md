# ClientSupportSystem
This is a test task for Moneybase.

The project contains of the following components:
* a simple client app that sends a request for creating a new chat and keeps pinging once it gets a successful response.
* the client API that receives client's requests and redirects them to the management service
* the management service that holds the logic sessions management and allocation them to existing agents.

## Notes
* It was not clear from the task description should an overflow team influence to the total capacity. I believe it should not because otherwise the capacity (and maximum queue) would grow significantly what doesn't seem to be right in a real life.
* Also it's not clear should already allocated chats be counted with the oned stored in queue when it comes to a decision should a new session be refused. My idea is they should not, since the queue is independant storage, but it's easy to change this logic just updtating the maximum queue size formula.
