import { Typography } from '@equinor/eds-core-react'
import { TranslateText } from 'components/Contexts/LanguageContext'
import { Robot } from 'models/Robot'
import { LocalizationDialog } from './LocalizationDialog'

interface RobotProps {
    robot: Robot
}

export function LocalizationSection({ robot }: RobotProps) {

    return (
        <>
            <Typography variant="h2">{TranslateText('Localization')}</Typography>
            <LocalizationDialog robot={robot} />
        </>
    )
}
