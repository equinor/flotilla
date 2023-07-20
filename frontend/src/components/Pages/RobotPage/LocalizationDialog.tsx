import { Autocomplete, AutocompleteChanges, Button, Card, Dialog, Typography, Icon } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'
import { useState, useEffect } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'
import { AssetDeck } from 'models/AssetDeck'
import { Robot } from 'models/Robot'
import { AssetDeckMapView } from './AssetDeckMapView'
import { Pose } from 'models/Pose'
import { Orientation } from 'models/Orientation'
import { Mission, MissionStatus } from 'models/Mission'
import { tokens } from '@equinor/eds-tokens'

const StyledDialog = styled(Card)`
    display: flex;
    padding: 1rem;
    width: 600px;
    right: 175px;
`

const StyledAutoComplete = styled.div`
    display: flex;
    flex-direction: row;
    justify-content: space-evenly;
`

const StyledLocalization = styled.div`
    display: flex;
    gap: 8px;
`

const StyledButtons = styled.div`
    display: flex;
    gap: 8px;
    justify-content: flex-end;
`

const StyledCard = styled(Card)`
    display: flex;
    justify-content: center;
    align-items: center;
    max-width: 300px;
    height: 36px;s
`

interface RobotProps {
    robot: Robot
}

export const LocalizationDialog = ({ robot }: RobotProps): JSX.Element => {
    const [isLocalizationDialogOpen, setIsLocalizationDialogOpen] = useState<boolean>(false)
    const [missionLocalizationStatus, setMissionLocalizationInfo] = useState<string>()
    const [selectedAssetDeck, setSelectedAssetDeck] = useState<AssetDeck>()
    const [assetDecks, setAssetDecks] = useState<AssetDeck[]>()
    const [localizationPose, setLocalizationPose] = useState<Pose>()
    const [selectedDirection, setSelectedDirecion] = useState<Orientation>()
    const [localizing, setLocalising] = useState<Boolean>(false)
    const { translate } = useLanguageContext()

    const colorGreen = '#A1DAA0'
    const colorGreenToken = tokens.colors.text.static_icons__default.hex

    const directionMap: Map<string, Orientation> = new Map([
        [translate('North'), { x: 0, y: 0, z: 0.7071, w: 0.7071 }],
        [translate('East'), { x: 0, y: 0, z: 0, w: 1 }],
        [translate('South'), { x: 0, y: 0, z: -0.7071, w: 0.7071 }],
        [translate('West'), { x: 0, y: 0, z: 1, w: 0 }],
    ])

    useEffect(() => {
        BackendAPICaller.getAssetDecks().then((response: AssetDeck[]) => {
            setAssetDecks(response)
        })
    }, [])

    useEffect(() => {
        if (selectedAssetDeck && localizationPose && localizing) {
            BackendAPICaller.postLocalizationMission(localizationPose, robot.id, selectedAssetDeck.id)
                .then((result: unknown) => result as Mission)
                .then(async (mission: Mission) => {
                    BackendAPICaller.getMissionById(mission.id)
                    while (mission.status == MissionStatus.Ongoing || mission.status == MissionStatus.Pending) {
                        mission = await BackendAPICaller.getMissionById(mission.id)
                    }
                    setLocalising(false)
                    return mission
                })
                .then((mission: Mission) => setMissionLocalizationInfo(mission.status))
                .catch((e) => {
                    console.error(e)
                })
            onLocalizationDialogClose()
        }
    }, [localizing])

    const getAssetDeckNames = (assetDecks: AssetDeck[]): Map<string, AssetDeck> => {
        var assetDeckNameMap = new Map<string, AssetDeck>()
        assetDecks.forEach((assetDeck: AssetDeck) => {
            assetDeckNameMap.set(assetDeck.deckName, assetDeck)
        })
        return assetDeckNameMap
    }

    const onSelectedDeck = (changes: AutocompleteChanges<string>) => {
        const selectedDeckName = changes.selectedItems[0]
        const selectedAssetDeck = assetDecks?.find((assetDeck) => assetDeck.deckName === selectedDeckName)
        setSelectedAssetDeck(selectedAssetDeck)
        let newPose = selectedAssetDeck?.defaultLocalizationPose
        if (newPose && selectedDirection) {
            newPose.orientation = selectedDirection
        }
        setLocalizationPose(newPose)
    }

    const onSelectedDirection = (changes: AutocompleteChanges<string>) => {
        const selectedDirection = directionMap.get(changes.selectedItems[0])
        setSelectedDirecion(selectedDirection)
        let newPose = localizationPose
        if (newPose && selectedDirection) {
            newPose.orientation = selectedDirection
            setLocalizationPose(newPose)
        }
    }

    const onClickLocalizeRobot = () => {
        setIsLocalizationDialogOpen(true)
    }

    const onLocalizationDialogClose = () => {
        setIsLocalizationDialogOpen(false)
        setSelectedAssetDeck(undefined)
    }

    const onClickLocalize = async () => {
        setMissionLocalizationInfo(undefined)
        setLocalising(true)
    }

    const assetDeckNames = assetDecks ? Array.from(getAssetDeckNames(assetDecks).keys()).sort() : []
    return (
        <>
            <StyledLocalization>
                <Button
                    onClick={() => {
                        onClickLocalizeRobot()
                    }}
                >
                    <>
                        <Icon name={Icons.PinDrop} size={16} />
                        {translate('Localize robot')}
                    </>
                </Button>
                {(localizing || missionLocalizationStatus) && (
                    <>
                        {!missionLocalizationStatus && (
                            <StyledCard variant="info">
                                <StyledCard.Header>
                                    <Typography variant="body_short">{translate('Localizing') + '...'}</Typography>
                                </StyledCard.Header>
                            </StyledCard>
                        )}
                        {missionLocalizationStatus == MissionStatus.Successful && (
                            <StyledCard style={{ background: colorGreen, color: colorGreenToken }}>
                                <StyledCard.Header>
                                    <Typography variant="body_short">
                                        {translate('Localization') +
                                            ' ' +
                                            translate(missionLocalizationStatus).toLocaleLowerCase()}
                                    </Typography>
                                </StyledCard.Header>
                            </StyledCard>
                        )}
                        {missionLocalizationStatus && missionLocalizationStatus !== MissionStatus.Successful && (
                            <StyledCard variant="danger">
                                <StyledCard.Header>
                                    <Typography variant="body_short">
                                        {translate('Localization') +
                                            ' ' +
                                            translate(missionLocalizationStatus).toLocaleLowerCase()}
                                    </Typography>
                                </StyledCard.Header>
                            </StyledCard>
                        )}
                    </>
                )}
            </StyledLocalization>
            <Dialog open={isLocalizationDialogOpen} isDismissable>
                <StyledDialog>
                    <Typography variant="h2">{translate('Localize robot')}</Typography>
                    <StyledAutoComplete>
                        <Autocomplete
                            options={assetDeckNames}
                            label={translate('Select deck')}
                            onOptionsChange={onSelectedDeck}
                        />
                        <Autocomplete
                            options={Array.from(directionMap.keys())}
                            label={translate('Select direction')}
                            onOptionsChange={onSelectedDirection}
                        />
                    </StyledAutoComplete>
                    {selectedAssetDeck && localizationPose && (
                        <AssetDeckMapView
                            assetDeck={selectedAssetDeck}
                            localizationPose={localizationPose}
                            setLocalizationPose={setLocalizationPose}
                        />
                    )}
                    <StyledButtons>
                        <Button
                            onClick={() => {
                                onLocalizationDialogClose()
                            }}
                            variant="outlined"
                            color="secondary"
                        >
                            {' '}
                            {translate('Cancel')}{' '}
                        </Button>
                        <Button onClick={onClickLocalize} disabled={!selectedAssetDeck}>
                            {' '}
                            {translate('Localize')}{' '}
                        </Button>
                    </StyledButtons>
                </StyledDialog>
            </Dialog>
        </>
    )
}
