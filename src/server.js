import express from 'express';
import cors from 'cors';

const app = express();
const port = 3000;

// Enable CORS and JSON parsing
app.use(cors());
app.use(express.json());

// Health check endpoint
app.get('/health', (req, res) => {
    res.json({ status: 'ok' });
});

// Helper function to simulate delay
const sleep = (ms) => new Promise(resolve => setTimeout(resolve, ms));

// Mock TypeScript content
const tsContent1 = `import * as fs from 'fs';

function calculateFloor(input: string): number {
    let floor: number = 0;
    for (let i: number = 0; i < input.length; i++) {
        if (input[i] === '(') {
            floor++;
        } else if (input[i] === ')') {
            floor--;
        }
    }
    return floor;
}

const input: string = fs.readFileSync('input.txt', 'utf8');
console.log('Part 1:', calculateFloor(input));`;

const tsContent2 = `import * as fs from 'fs';

function findBasementPosition(input: string): number {
    let floor: number = 0;
    for (let i: number = 0; i < input.length; i++) {
        if (input[i] === '(') {
            floor++;
        } else if (input[i] === ')') {
            floor--;
        }
        if (floor === -1) {
            return i + 1;
        }
    }
    return -1;
}

const input: string = fs.readFileSync('input.txt', 'utf8');
console.log('Part 2:', findBasementPosition(input));`;

const convertedFiles = [
    { oldPath: "2015/day1.js", newPath: "2015/day1-converted-1.ts", content: tsContent1 },
    { oldPath: "2015/day1.js", newPath: "2015/day1-converted-2.ts", content: tsContent2 },
    { oldPath: "2015/day2.ts", newPath: "2015/day2-converted.ts", content: tsContent1 },
    { oldPath: "2015/day4.js", newPath: "2015/day4-converted.ts", content: tsContent2 },
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
            text: "FROM Node server, I'll help you convert the file. Let me analyze the content and make the necessary changes.\n\n"
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
            text: "<attempt_completion>\n<result>\nI've successfully converted the JavaScript files to TypeScript. The conversion includes:\n- Proper TypeScript type annotations\n- Modern ES6+ import syntax\n- Type declarations for all variables and functions\n- Maintained the original functionality while adding type safety\n</result>\n</attempt_completion>"
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

// Start the server
app.listen(port, () => {
    console.log(`Mock LegacyLeap API server running`);
}); 