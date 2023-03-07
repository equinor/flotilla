import { UnauthorizedPage } from './UnauthorizedPage/UnauthorizedPage'

export const ErrorFallback = (error: Error) => {
    switch (error.message) {
        case '403 - ': {
            return <UnauthorizedPage />
        }
        default: {
            return (
                <>
                    Type = {error.name} - {error.message} - {error.cause}
                </>
            )
        }
    }
}
