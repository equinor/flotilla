run:
	pnpm dev

build:
	pnpm build

format:
	pnpm exec prettier --write src tests
	pnpm exec eslint src tests
	pnpm exec ts-unused-exports tsconfig.json
