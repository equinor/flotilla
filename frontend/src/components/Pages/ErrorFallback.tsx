import { UnauthorizedPage } from './UnauthorizedPage/UnauthorizedPage'
import { UnknownErrorPage } from './UnknownErrorPage/UnknownErrorPage'

export const ErrorFallback = (error: Error) => {
    switch (error.message) {
        case '403 - ': {
            return <UnauthorizedPage />
        }
        default: {
            return <UnknownErrorPage />
        }
    }
}
