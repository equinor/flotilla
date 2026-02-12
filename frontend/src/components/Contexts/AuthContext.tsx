import { createContext } from 'react'

type AuthContextValue = {
    getAccessToken: () => Promise<string>
    isAuthenticated: boolean
}

export const AuthContext = createContext<AuthContextValue>({
    isAuthenticated: false,
    getAccessToken: async () => {
        throw new Error('AuthProvider not mounted')
    },
})
