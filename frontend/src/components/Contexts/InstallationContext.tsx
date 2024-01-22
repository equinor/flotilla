import { createContext, FC, useContext, useState, useEffect } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'
import { EchoPlantInfo } from 'models/EchoMission'
import { Deck } from 'models/Deck'
import { SignalREventLabels, useSignalRContext } from './SignalRContext'
import { Area } from 'models/Area'

interface IInstallationContext {
    installationCode: string
    installationName: string
    installationDecks: Deck[]
    installationAreas: Area[]
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
    installationAreas: [],
    switchInstallation: (selectedInstallation: string) => {},
}

export const InstallationContext = createContext<IInstallationContext>(defaultInstallation)

export const InstallationProvider: FC<Props> = ({ children }) => {
    const { registerEvent, connectionReady } = useSignalRContext()
    const [allPlantsMap, setAllPlantsMap] = useState<Map<string, string>>(new Map())
    const [installationName, setInstallationName] = useState<string>(
        window.localStorage.getItem('installationName') || ''
    )
    const [installationDecks, setInstallationDecks] = useState<Deck[]>([])
    const [installationAreas, setInstallationAreas] = useState<Area[]>([])

    const installationCode = allPlantsMap.get(installationName) || ''

    useEffect(() => {
        BackendAPICaller.getEchoPlantInfo().then((response: EchoPlantInfo[]) => {
            const mapping = mapInstallationCodeToName(response)
            setAllPlantsMap(mapping)
        })
    }, [])

    useEffect(() => {
        if (installationCode)
            BackendAPICaller.getDecksByInstallationCode(installationCode).then((decks: Deck[]) => {
                setInstallationDecks(decks)
                decks.forEach((deck) =>
                    BackendAPICaller.getAreasByDeckId(deck.id).then((areas) =>
                        setInstallationAreas((oldAreas) => {
                            let areasCopy = [...oldAreas]
                            let newAreas: Area[] = []
                            areas.forEach((area) => {
                                const indexBeUpdated = areasCopy.findIndex((a) => a.id === area.id)
                                if (indexBeUpdated === -1) newAreas = [...newAreas, area]
                                else areasCopy[indexBeUpdated] = area
                            })
                            return areasCopy.concat(newAreas)
                        })
                    )
                )
            })
    }, [installationCode])

    useEffect(() => {
        if (connectionReady) {
            registerEvent(SignalREventLabels.deckCreated, (username: string, message: string) => {
                const newDeck: Deck = JSON.parse(message)
                if (newDeck.installationCode !== installationCode) return
                setInstallationDecks((oldDecks) => {
                    return [...oldDecks, newDeck]
                })
            })
            registerEvent(SignalREventLabels.deckUpdated, (username: string, message: string) => {
                const updatedDeck: Deck = JSON.parse(message)
                if (updatedDeck.installationCode !== installationCode) return

                setInstallationDecks((oldDecks) => {
                    const deckIndex = oldDecks.findIndex((d) => d.id === updatedDeck.id)
                    if (deckIndex === -1) return [...oldDecks, updatedDeck]
                    else {
                        let oldDecksCopy = [...oldDecks]
                        oldDecksCopy[deckIndex] = updatedDeck
                        return oldDecksCopy
                    }
                })
            })
            registerEvent(SignalREventLabels.deckDeleted, (username: string, message: string) => {
                const deletedDeck: Deck = JSON.parse(message)
                if (deletedDeck.installationCode !== installationCode) return
                setInstallationDecks((oldDecks) => {
                    const deckIndex = oldDecks.findIndex((d) => d.id === deletedDeck.id)
                    if (deckIndex !== -1) {
                        let oldDecksCopy = [...oldDecks]
                        oldDecksCopy.splice(deckIndex, 1)
                        return oldDecksCopy
                    }
                    return oldDecks
                })
            })
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [registerEvent, connectionReady])

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
                installationAreas,
                switchInstallation,
            }}
        >
            {children}
        </InstallationContext.Provider>
    )
}

export const useInstallationContext = () => useContext(InstallationContext)
