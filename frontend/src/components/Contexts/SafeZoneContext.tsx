import { createContext, FC, useContext, useState } from 'react'

interface ISafeZoneContext {
    safeZoneStatus: boolean
    switchSafeZoneStatus: (newSafeZoneStatus: boolean) => void
}

interface Props {
    children: React.ReactNode
}

const defaultSafeZoneInterface = {
    safeZoneStatus: JSON.parse(localStorage.getItem('safeZoneStatus') ?? 'false'),
    switchSafeZoneStatus: (newSafeZoneStatus: boolean) => {},
}

export const SafeZoneContext = createContext<ISafeZoneContext>(defaultSafeZoneInterface)

export const SafeZoneProvider: FC<Props> = ({ children }) => {
    const [safeZoneStatus, setSafeZoneStatus] = useState<boolean>(defaultSafeZoneInterface.safeZoneStatus)

    const switchSafeZoneStatus = (newSafeZoneStatus: boolean) => {
        localStorage.setItem('safeZoneStatus', String(newSafeZoneStatus))
        setSafeZoneStatus(newSafeZoneStatus)
    }

    return (
        <SafeZoneContext.Provider
            value={{
                safeZoneStatus,
                switchSafeZoneStatus,
            }}
        >
            {children}
        </SafeZoneContext.Provider>
    )
}

export const useSafeZoneContext = () => useContext(SafeZoneContext)
