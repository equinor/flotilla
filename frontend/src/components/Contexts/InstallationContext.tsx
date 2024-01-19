import { createContext, FC, useContext, useState, useEffect } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'
import { EchoPlantInfo } from 'models/EchoMission'
import { Deck } from 'models/Deck'

interface IInstallationContext {
    installationCode: string
    installationName: string
    installationDecks: Deck[]
    switchInstallation: (selectedName: string) => void
}

const mapInstallationCodeToName = (echoPlantInfoArray: EchoPlantInfo[]): Map<string, string> => {
    var mapping = new Map<string, string>()
    echoPlantInfoArray.forEach((echoPlantInfo: EchoPlantInfo) => {
        mapping.set(echoPlantInfo.projectDescription, echoPlantInfo.plantCode)
    })
    return mapping
}

interface Props {
    children: React.ReactNode
}

const defaultInstallation = {
    installationCode: '',
    installationName: '',
    installationDecks: [],
    switchInstallation: (selectedInstallation: string) => {},
}

export const InstallationContext = createContext<IInstallationContext>(defaultInstallation)

export const InstallationProvider: FC<Props> = ({ children }) => {
    const [allPlantsMap, setAllPlantsMap] = useState<Map<string, string>>(new Map())
    const [installationName, setInstallationName] = useState<string>(
        window.localStorage.getItem('installationName') || ''
    )
    const [installationDecks, setInstallationDecks] = useState<Deck[]>([])

    const installationCode = allPlantsMap.get(installationName) || ''

    useEffect(() => {
        BackendAPICaller.getEchoPlantInfo().then((response: EchoPlantInfo[]) => {
            const mapping = mapInstallationCodeToName(response)
            setAllPlantsMap(mapping)
        })
    }, [])

    useEffect(() => {
        BackendAPICaller.getDecksByInstallationCode(installationCode).then(async (decks: Deck[]) => {
            setInstallationDecks(decks)
        })
    }, [installationCode])

    const switchInstallation = (selectedName: string) => {
        setInstallationName(selectedName)
        window.localStorage.setItem('installationName', selectedName)
        const derivedCode = allPlantsMap.get(selectedName) || ''
        window.localStorage.setItem('installationCode', derivedCode)
    }

    return (
        <InstallationContext.Provider
            value={{
                installationCode,
                installationName,
                installationDecks,
                switchInstallation,
            }}
        >
            {children}
        </InstallationContext.Provider>
    )
}

export const useInstallationContext = () => useContext(InstallationContext)
