import {
    Autocomplete,
    AutocompleteChanges,
    Button,
    Card,
    Dialog,
    Icon,
    Search,
    TextField,
    Typography,
} from '@equinor/eds-core-react'
import styled from 'styled-components'
import { TranslateText } from 'components/Contexts/LanguageContext'
import { MissionStatus } from 'models/Mission'
import { ChangeEvent, useState } from 'react'
import { Icons } from 'utils/icons'
import { InspectionType } from 'models/Inspection'
import { useMissionFilterContext } from 'components/Contexts/MissionFilterContext'

const StyledHeader = styled.div`
    display: flex;
    flex-direction: row;
    justify-content: space-between;
    gap: 1rem;
`

const StyledDialog = styled(Card)`
    display: flex;
    padding: 1rem;
    width: 600px;
    right: 175px;
`

export function FilterSection() {
    const [isFilteringDialogOpen, setIsFilteringDialogOpen] = useState<boolean>(false)
    const [formattedMinStartTime, setFormattedMinStartTime] = useState<string | undefined>(undefined)
    const [formattedMaxStartTime, setFormattedMaxStartTime] = useState<string | undefined>(undefined)
    const [formattedMinEndTime, setFormattedMinEndTime] = useState<string | undefined>(undefined)
    const [formattedMaxEndTime, setFormattedMaxEndTime] = useState<string | undefined>(undefined)
    const {
        missionName,
        switchMissionName,
        statuses,
        switchStatuses,
        robotName,
        switchRobotName,
        tagId,
        switchTagId,
        inspectionTypes,
        switchInspectionTypes,
        switchMinStartTime,
        switchMaxStartTime,
        switchMinEndTime,
        switchMaxEndTime,
        resetFilters,
    } = useMissionFilterContext()

    const missionStatusTranslationMap: Map<string, MissionStatus> = new Map(
        Object.values(MissionStatus).map((missionStatus) => {
            return [TranslateText(missionStatus), missionStatus]
        })
    )

    const inspectionTypeTranslationMap: Map<string, InspectionType> = new Map(
        Object.values(InspectionType).map((inspectionType) => {
            return [TranslateText(inspectionType), inspectionType]
        })
    )

    const changeMinStartTime = (newMinStartTime: string | undefined) => {
        switchMinStartTime(newMinStartTime ? new Date(newMinStartTime).getTime() / 1000 : undefined)
        setFormattedMinStartTime(newMinStartTime)
    }

    const changeMaxStartTime = (newMaxStartTime: string | undefined) => {
        switchMaxStartTime(newMaxStartTime ? new Date(newMaxStartTime).getTime() / 1000 : undefined)
        setFormattedMaxStartTime(newMaxStartTime)
    }

    const changeMinEndTime = (newMinEndTime: string | undefined) => {
        switchMinEndTime(newMinEndTime ? new Date(newMinEndTime).getTime() / 1000 : undefined)
        setFormattedMinEndTime(newMinEndTime)
    }

    const changeMaxEndTime = (newMaxEndTime: string | undefined) => {
        switchMaxEndTime(newMaxEndTime ? new Date(newMaxEndTime).getTime() / 1000 : undefined)
        setFormattedMaxEndTime(newMaxEndTime)
    }

    const onClickFilterIcon = () => {
        setIsFilteringDialogOpen(true)
    }

    const onFilterClose = () => {
        setIsFilteringDialogOpen(false)
    }

    const onClearFilters = () => {
        resetFilters()
    }

    return (
        <>
            <StyledHeader>
                <Search
                    value={missionName ?? ''}
                    placeholder={TranslateText('Search for missions')}
                    onChange={(changes: ChangeEvent<HTMLInputElement>) => {
                        switchMissionName(changes.target.value)
                    }}
                />
                <Button onClick={onClickFilterIcon}>
                    <Icon name={Icons.Filter} size={32} />
                    {TranslateText('Filter')}
                </Button>
                <Button variant="outlined" onClick={onClearFilters}>
                    <Icon name={Icons.Clear} size={32} />
                    {TranslateText('Clear all filters')}
                </Button>
            </StyledHeader>
            <Dialog open={isFilteringDialogOpen} isDismissable>
                <StyledDialog>
                    <StyledHeader>
                        <Typography variant="h2">{TranslateText('Filter')}</Typography>
                        <Button variant="ghost_icon" onClick={onFilterClose}>
                            <Icon name={Icons.Clear} size={32} />
                        </Button>
                    </StyledHeader>
                    <Autocomplete
                        options={Array.from(missionStatusTranslationMap.keys())}
                        onOptionsChange={(changes: AutocompleteChanges<string>) => {
                            switchStatuses(
                                changes.selectedItems.map((selectedItem) => {
                                    return missionStatusTranslationMap.get(selectedItem)!
                                })
                            )
                        }}
                        placeholder={`${statuses.length}/${
                            Array.from(missionStatusTranslationMap.keys()).length
                        } ${TranslateText('selected')}`}
                        label={TranslateText('Mission status')}
                        initialSelectedOptions={statuses.map((status) => {
                            return TranslateText(status)
                        })}
                        multiple
                    ></Autocomplete>
                    <Autocomplete
                        options={Array.from(inspectionTypeTranslationMap.keys())}
                        onOptionsChange={(changes: AutocompleteChanges<string>) => {
                            switchInspectionTypes(
                                changes.selectedItems.map((selectedItem) => {
                                    return inspectionTypeTranslationMap.get(selectedItem)!
                                })
                            )
                        }}
                        placeholder={`${inspectionTypes.length}/${
                            Array.from(inspectionTypeTranslationMap.keys()).length
                        } ${TranslateText('selected')}`}
                        label={TranslateText('Inspection type')}
                        initialSelectedOptions={inspectionTypes.map((inspectionType) => {
                            return TranslateText(inspectionType)
                        })}
                        multiple
                    ></Autocomplete>
                    <Search
                        value={robotName ?? ''}
                        placeholder={TranslateText('Search for a robot name')}
                        onChange={(changes: ChangeEvent<HTMLInputElement>) => {
                            switchRobotName(changes.target.value)
                        }}
                    />
                    <Search
                        value={tagId ?? ''}
                        placeholder={TranslateText('Search for a tag')}
                        onChange={(changes: ChangeEvent<HTMLInputElement>) => {
                            switchTagId(changes.target.value)
                        }}
                    />
                    <TextField
                        id="datetime"
                        value={formattedMinStartTime}
                        label={TranslateText('Select min start time')}
                        type="datetime-local"
                        onChange={(changes: ChangeEvent<HTMLInputElement>) => {
                            changeMinStartTime(changes.target.value)
                        }}
                    />
                    <TextField
                        id="datetime"
                        value={formattedMaxStartTime}
                        label={TranslateText('Select max start time')}
                        type="datetime-local"
                        onChange={(changes: ChangeEvent<HTMLInputElement>) => {
                            changeMaxStartTime(changes.target.value)
                        }}
                    />
                    <TextField
                        id="datetime"
                        value={formattedMinEndTime}
                        label={TranslateText('Select min end time')}
                        type="datetime-local"
                        onChange={(changes: ChangeEvent<HTMLInputElement>) => {
                            changeMinEndTime(changes.target.value)
                        }}
                    />
                    <TextField
                        id="datetime"
                        value={formattedMaxEndTime}
                        label={TranslateText('Select max end time')}
                        type="datetime-local"
                        onChange={(changes: ChangeEvent<HTMLInputElement>) => {
                            changeMaxEndTime(changes.target.value)
                        }}
                    />
                </StyledDialog>
            </Dialog>
        </>
    )
}
