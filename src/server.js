import express from 'express';
import cors from 'cors';
import fs from 'fs';

const app = express();
const port = 5000;

// Enable CORS and JSON parsing
app.use(cors());
app.use(express.json());

// Health check endpoint
app.get('/health', (req, res) => {
    res.json({ status: 'ok' });
});

// Helper function to simulate delay
const sleep = (ms) => new Promise(resolve => setTimeout(resolve, ms));

const convertedFiles = [
    { oldPath: "legacy_code/AS-Auth/AS-Auth.csproj", newPath: "modernize/AS-Auth/AS-AuthMigrated.csproj", content: fs.readFileSync("src/AS-Auth/AS-Auth.csproj", "utf8") },
    { oldPath: "legacy_code/AS-Auth/Auth.cs", newPath: "modernize/AS-Auth/AuthMigrated.cs", content: fs.readFileSync("src/AS-Auth/Auth.cs", "utf8") },
    { oldPath: "legacy_code/AS-Auth/KeyUtil.cs", newPath: "modernize/AS-Auth/KeyUtilMigrated.cs", content: fs.readFileSync("src/AS-Auth/KeyUtil.cs", "utf8") },
    { oldPath: "legacy_code/AS-Auth/DateTimeOffsetProvider.cs", newPath: "modernize/AS-Auth/DateTimeOffsetProviderMigrated.cs", content: fs.readFileSync("src/AS-Auth/DateTimeOffsetProvider.cs", "utf8") },
    { oldPath: "legacy_code/AS-Auth/GuidProvider.cs", newPath: "modernize/AS-Auth/GuidProviderMigrated.cs", content: fs.readFileSync("src/AS-Auth/GuidProvider.cs", "utf8") },
];

// Streaming API endpoint
app.post('/api/legacyleap', async (req, res) => {
    // Set headers for SSE
    res.setHeader('Content-Type', 'text/event-stream');
    res.setHeader('Cache-Control', 'no-cache');
    res.setHeader('Connection', 'keep-alive');

    try {
        // Initial message
        res.write(`data: ${JSON.stringify({
            type: 'text',
            text: "Streaming the conversion of the .Net 4 to .Net 10.\n\n"
        })}\n\n`);
        await sleep(300);

        // Stream each file conversion
        for (const file of convertedFiles) {
            // Start the show_converted_file tool XML
            res.write(`data: ${JSON.stringify({
                type: 'text',
                text: `<show_converted_file><old_path>${file.oldPath}</old_path><new_path>${file.newPath}</new_path>\n<content>`
            })}\n\n`);
            await sleep(300);

            // Stream content line by line
            const contentLines = file.content.split('\n');
            for (const line of contentLines) {
                res.write(`data: ${JSON.stringify({
                    type: 'text',
                    text: line + '\n'
                })}\n\n`);
                await sleep(50 + Math.random() * 100);
            }

            res.write(`data: ${JSON.stringify({
                type: 'text',
                text: "</content></show_converted_file>\n\n"
            })}\n\n`);
            await sleep(300);
        }

        // Final completion message
        res.write(`data: ${JSON.stringify({
            type: 'text',
            text: "<attempt_completion>\n<result>\nConversion complete.\n</result>\n</attempt_completion>"
        })}\n\n`);

        // Add usage statistics
        res.write(`data: ${JSON.stringify({
            type: 'usage',
            inputTokens: 300,
            outputTokens: 400
        })}\n\n`);

        // End the stream
        res.end();
    } catch (error) {
        console.error('Error in streaming:', error);
        res.end();
    }
});

// Helper function to verify authorization
const verifyAuthorization = (req, res, next) => {
    const authHeader = req.headers.authorization;
    
    if (!authHeader || !authHeader.startsWith('Bearer ')) {
        return res.status(401).json({ 
            error: 'Unauthorized', 
            message: 'Authorization Bearer token required' 
        });
    }
    
    const token = authHeader.substring(7); // Remove 'Bearer ' prefix
    
    // In a real implementation, you would verify the token here
    if (!token || token.trim() === '') {
        return res.status(401).json({ 
            error: 'Unauthorized', 
            message: 'Invalid Bearer token' 
        });
    }
    
    // For this mock server, we'll accept any non-empty token
    req.bearerToken = token;
    next();
};

// Model info endpoint - returns model information with JWT encrypted API keys
app.get('/api/legacyleap/model_info', verifyAuthorization, (req, res) => {
    try {
        // Create mock JWT tokens similar to legacyleap.ts getMockModelInfo method
        const algorithm = 'HS256';
        const modelInfo = {
            legacyLeapSupportedModels: {
                "gpt-4o": {
                    provider: "openai",
                    encryptedApiKey: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhcGlLZXkiOiJzay1wcm9qLVdjYlVjT0dlM0xscFlySzIzazlpbUY1a2NSY2l2RU03WjJjV1hKTGNVcFd1d2tKNEIzZ09tb2M2WTl1RjhkX3pkYlhoclF2UE15VDNCbGJrRkpsbjdpcnp1NzV0dUxaVWpLR0NwTDZwek1jVDYxbnFvTGZPTEFlempPX2tVeThwbTU5cHpDNndnYnpHOHFxVkU3bzZiTU9WNHdFQSIsImlzcyI6ImxlZ2FjeWxlYXAiLCJpYXQiOjE3NDk3MTY1MjF9.FUDknzR2PN6xb6y4PAzx47W8hQvs8LcD0l3Br7TT2Y8",
                    maxTokens: 4_096,
                    contextWindow: 128_000,
                    supportsImages: true,
                    supportsPromptCache: false,
                    inputPrice: 2.5,
                    outputPrice: 10,
                },
                "legacyleap-1.0-standard": {
                    provider: "legacyLeap",
                    encryptedApiKey: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhcGlLZXkiOiJtb2NrLWxlZ2FjeWxlYXAta2V5IiwiaXNzIjoibGVnYWN5bGVhcCIsImlhdCI6MTc0OTcxNjUyMX0.Yft6eQKpBC-o51vJH5Zlit4A6HTHQCzM5-FeHZW8XY8",
                    maxTokens: 4_096,
                    contextWindow: 128_000,
                    supportsImages: true,
                    supportsPromptCache: false,
                    inputPrice: 2.5,
                    outputPrice: 10,
                }
            },
            algorithm
        };
        
        res.json(modelInfo);
    } catch (error) {
        console.error('Error generating model info:', error);
        res.status(500).json({ 
            error: 'Internal Server Error', 
            message: 'Failed to generate model information' 
        });
    }
});

// Start the server
app.listen(port, () => {
    console.log(`Mock LegacyLeap API server running`);
}); 