import { EchoPlantInfo } from 'models/EchoMission'
import { createContext, FC, useContext, useState } from 'react'

interface IInstallationContext {
    currentPlant: EchoPlantInfo | undefined
    setPlant: (selectedEchoPlant: EchoPlantInfo | undefined) => void
}

interface Props {
    children: React.ReactNode
}

const defaultInstallation = {
    currentPlant: undefined,
    setPlant: (selectedEchoPlant: EchoPlantInfo | undefined) => {},
}

export const InstallationContext = createContext<IInstallationContext>(defaultInstallation)

export const InstallationProvider: FC<Props> = ({ children }) => {
    const localStoragePlant = window.localStorage.getItem('plant')
    const previousPlant =
        localStoragePlant && localStoragePlant != 'undefined' ? JSON.parse(localStoragePlant) : undefined
    const [currentPlant, setCurrentPlant] = useState<EchoPlantInfo | undefined>(
        previousPlant || defaultInstallation.currentPlant
    )

    const setPlant = (selectedEchoPlant: EchoPlantInfo | undefined) => {
        setCurrentPlant(selectedEchoPlant)
        window.localStorage.setItem('plant', JSON.stringify(selectedEchoPlant))
    }

    return (
        <InstallationContext.Provider
            value={{
                currentPlant,
                setPlant,
            }}
        >
            {children}
        </InstallationContext.Provider>
    )
}

export const usePlantContext = () => useContext(InstallationContext)
