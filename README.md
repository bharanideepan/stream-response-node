# LegacyLeap Lambda Function

This is a Lambda-optimized version of the LegacyLeap API that handles file conversion requests with streaming support.

## Prerequisites

- AWS Account
- AWS CLI configured
- Node.js 18.x or later
- npm or yarn

## Setup and Deployment

1. Install dependencies:
```bash
npm install
```

2. Create a deployment package:
```bash
zip -r function.zip . -x "*.git*" "node_modules/*"
```

3. Create the Lambda function in AWS Console:
   - Runtime: Node.js 18.x
   - Architecture: x86_64
   - Handler: lambda-handler.handler
   - Memory: 256 MB (adjust based on needs)
   - Timeout: 30 seconds (adjust based on needs)

4. Configure API Gateway:
   - Create a new HTTP API
   - Add a POST route for `/api/legacyleap`
   - Enable CORS
   - Enable response streaming
   - Deploy the API

## Environment Variables

No environment variables are required for basic functionality.

## API Usage

Send a POST request to your API Gateway endpoint:

```bash
curl -N -X POST https://your-api-gateway-url/api/legacyleap
```

Or using JavaScript with EventSource:
```javascript
const eventSource = new EventSource('https://your-api-gateway-url/api/legacyleap');
eventSource.onmessage = (event) => {
    console.log(JSON.parse(event.data));
};
```

The response will be streamed in Server-Sent Events (SSE) format.

## Response Format

The API streams responses in SSE format with the following event types:

1. `text`: General messages and file content
2. `usage`: Usage statistics

Example streamed response:
```
data: {"type":"text","text":"FROM Lambda function..."}
data: {"type":"text","text":"<show_converted_files>..."}
data: {"type":"usage","inputTokens":300,"outputTokens":400}
```

## Streaming Implementation

The Lambda function uses API Gateway's response streaming capability to:
- Stream responses in real-time
- Maintain connection for the duration of the conversion
- Provide immediate feedback to the client
- Handle large responses efficiently

## Limitations

1. Response size limit: 6MB
2. Maximum execution time: 15 minutes
3. Memory: 256MB-10GB (configurable)
4. API Gateway timeout: 30 seconds (default, can be increased)

## Error Handling

The function returns appropriate HTTP status codes:
- 200: Success (with streaming)
- 500: Internal server error

## CORS Support

CORS is enabled by default with the following configuration:
- Allowed Origins: *
- Allowed Methods: POST, OPTIONS
- Allowed Headers: Content-Type

## Monitoring

Monitor the function using:
- CloudWatch Logs
- CloudWatch Metrics
- X-Ray (if enabled)

## Cost Optimization

1. Adjust memory based on actual usage
2. Set appropriate timeout values
3. Consider using provisioned concurrency for consistent performance
4. Monitor streaming duration to optimize costs
