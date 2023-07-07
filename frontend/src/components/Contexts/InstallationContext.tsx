import { createContext, FC, useContext, useState } from 'react'

interface IInstallationContext {
    installationCode: string
    switchInstallation: (selectedInstallation: string) => void
}

interface Props {
    children: React.ReactNode
}

const defaultInstallation = {
    installationCode: '',
    switchInstallation: (selectedInstallation: string) => {},
}

export const InstallationContext = createContext<IInstallationContext>(defaultInstallation)

export const InstallationProvider: FC<Props> = ({ children }) => {
    const previousInstallation = window.localStorage.getItem('installationString')
    const [installationCode, setInstallation] = useState(previousInstallation || defaultInstallation.installationCode)

    const switchInstallation = (selectedInstallation: string) => {
        setInstallation(selectedInstallation.toLowerCase())
        window.localStorage.setItem('installationString', selectedInstallation.toLowerCase())
    }

    return (
        <InstallationContext.Provider
            value={{
                installationCode,
                switchInstallation,
            }}
        >
            {children}
        </InstallationContext.Provider>
    )
}

export const useInstallationContext = () => useContext(InstallationContext)
