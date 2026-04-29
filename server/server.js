import express from 'express';
import cors from 'cors';

const app = express();
app.use(cors());
app.use(express.json());

// In-memory store. Add your collections here.
const store = {
    // example: items: new Map(),
};

app.get('/health', (_req, res) => {
    res.json({ ok: true, service: 'mock-backend' });
});

const PORT = process.env.PORT ?? 3000;
app.listen(PORT, () => {
    console.log(`Mock server listening on http://localhost:${PORT}`);
});
