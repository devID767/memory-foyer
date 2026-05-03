import { z } from 'zod';

const isoDateTime = z.string().refine((s) => !Number.isNaN(Date.parse(s)), {
    message: 'must be a valid ISO-8601 datetime',
});

export const gradeSchema = z.union([z.literal(0), z.literal(3), z.literal(4), z.literal(5)]);

export const cardReviewSchema = z.object({
    cardId: z.string().min(1),
    grade: gradeSchema,
    reviewedAt: isoDateTime,
});

export const sessionResultSchema = z.object({
    sessionId: z.string().uuid(),
    deckId: z.string().min(1),
    reviews: z.array(cardReviewSchema),
});

export const deckIdParamSchema = z.object({ id: z.string().min(1) });
export const sessionIdParamSchema = z.object({ id: z.string().uuid() });
