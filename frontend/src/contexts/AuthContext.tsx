import { createContext } from 'react'

type AuthContextValue = {
    getBackendAccessToken: () => Promise<string>
    getSaraAccessToken: () => Promise<string>
    isAuthenticated: boolean
}

export const AuthContext = createContext<AuthContextValue>({
    isAuthenticated: false,
    getBackendAccessToken: async () => {
        throw new Error('AuthProvider not mounted')
    },
    getSaraAccessToken: async () => {
        throw new Error('AuthProvider not mounted')
    },
})
