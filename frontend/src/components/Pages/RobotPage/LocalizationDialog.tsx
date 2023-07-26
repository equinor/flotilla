import { Autocomplete, AutocompleteChanges, Button, Card, Dialog, Typography, Icon } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'
import { useState, useEffect } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'
import { Area } from 'models/Area'
import { Robot } from 'models/Robot'
import { AreaMapView } from './AreaMapView'
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
    const [selectedArea, setSelectedArea] = useState<Area>()
    const [areas, setAreas] = useState<Area[]>()
    const [localizationPose, setLocalizationPose] = useState<Pose>()
    const [selectedDirection, setSelectedDirecion] = useState<Orientation>()
    const [localizing, setLocalising] = useState<Boolean>(false)
    const { TranslateText } = useLanguageContext()

    const colorGreen = '#A1DAA0'
    const colorGreenToken = tokens.colors.text.static_icons__default.hex

    const directionMap: Map<string, Orientation> = new Map([
        [TranslateText('North'), { x: 0, y: 0, z: 0.7071, w: 0.7071 }],
        [TranslateText('East'), { x: 0, y: 0, z: 0, w: 1 }],
        [TranslateText('South'), { x: 0, y: 0, z: -0.7071, w: 0.7071 }],
        [TranslateText('West'), { x: 0, y: 0, z: 1, w: 0 }],
    ])

    useEffect(() => {
        BackendAPICaller.getAreas().then((response: Area[]) => {
            setAreas(response)
        })
    }, [])

    useEffect(() => {
        if (selectedArea && localizationPose && localizing) {
            BackendAPICaller.postLocalizationMission(localizationPose, robot.id, selectedArea.id)
                .then((result: unknown) => result as Mission)
                .then(async (mission: Mission) => {
                    BackendAPICaller.getMissionRunById(mission.id)
                    while (mission.status == MissionStatus.Ongoing || mission.status == MissionStatus.Pending) {
                        mission = await BackendAPICaller.getMissionRunById(mission.id)
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

    const getAreaNames = (areas: Area[]): Map<string, Area> => {
        var areaNameMap = new Map<string, Area>()
        areas.forEach((area: Area) => areaNameMap.set(area.deckName, area))
        return areaNameMap
    }

    const onSelectedDeck = (changes: AutocompleteChanges<string>) => {
        const selectedDeckName = changes.selectedItems[0]
        const selectedArea = areas?.find((area) => area.deckName === selectedDeckName)
        setSelectedArea(selectedArea)
        let newPose = selectedArea?.defaultLocalizationPose
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
        setSelectedArea(undefined)
    }

    const onClickLocalize = async () => {
        setMissionLocalizationInfo(undefined)
        setLocalising(true)
    }

    const areaNames = areas ? Array.from(getAreaNames(areas).keys()).sort() : []
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
                        {TranslateText('Localize robot')}
                    </>
                </Button>
                {(localizing || missionLocalizationStatus) && (
                    <>
                        {!missionLocalizationStatus && (
                            <StyledCard variant="info">
                                <StyledCard.Header>
                                    <Typography variant="body_short">{TranslateText('Localizing') + '...'}</Typography>
                                </StyledCard.Header>
                            </StyledCard>
                        )}
                        {missionLocalizationStatus == MissionStatus.Successful && (
                            <StyledCard style={{ background: colorGreen, color: colorGreenToken }}>
                                <StyledCard.Header>
                                    <Typography variant="body_short">
                                        {TranslateText('Localization') +
                                            ' ' +
                                            TranslateText(missionLocalizationStatus).toLocaleLowerCase()}
                                    </Typography>
                                </StyledCard.Header>
                            </StyledCard>
                        )}
                        {missionLocalizationStatus && missionLocalizationStatus !== MissionStatus.Successful && (
                            <StyledCard variant="danger">
                                <StyledCard.Header>
                                    <Typography variant="body_short">
                                        {TranslateText('Localization') +
                                            ' ' +
                                            TranslateText(missionLocalizationStatus).toLocaleLowerCase()}
                                    </Typography>
                                </StyledCard.Header>
                            </StyledCard>
                        )}
                    </>
                )}
            </StyledLocalization>
            <Dialog open={isLocalizationDialogOpen} isDismissable>
                <StyledDialog>
                    <Typography variant="h2">{TranslateText('Localize robot')}</Typography>
                    <StyledAutoComplete>
                        <Autocomplete
                            options={areaNames}
                            label={TranslateText('Select deck')}
                            onOptionsChange={onSelectedDeck}
                        />
                        <Autocomplete
                            options={Array.from(directionMap.keys())}
                            label={TranslateText('Select direction')}
                            onOptionsChange={onSelectedDirection}
                        />
                    </StyledAutoComplete>
                    {selectedArea && localizationPose && (
                        <AreaMapView
                            area={selectedArea}
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
                            {TranslateText('Cancel')}{' '}
                        </Button>
                        <Button onClick={onClickLocalize} disabled={!selectedArea}>
                            {' '}
                            {TranslateText('Localize')}{' '}
                        </Button>
                    </StyledButtons>
                </StyledDialog>
            </Dialog>
        </>
    )
}
