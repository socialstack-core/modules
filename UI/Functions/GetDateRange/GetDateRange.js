
export default function getDateRange(range){
    var now =  (Date.now());

    //This will map the number of days to each month (January starting at 0).
    var daysInMonth = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

    switch(range){
        case "Today":
            // now is in the range of Event Start and End
            return oneDay(now);

        case "Tomorrow":
            // now + 24hrs is in the range of Event Start and End
            var tomorrow =  now + 86400000;
            return oneDay(tomorrow);

        case "This Weekend":
            // Determine what day of the week it is. If its mon - thurs, calculate the next Friday
            // and the next Sunday. Return events where Start or EndDate is between those values. 
            // Let's convert timestamp to date so we can get day of the week
            day = new Date(now);
            weekDay = day.getUTCDay();
            // Is it monday thorugh thursday?
            if(0 > weekDay < 5){
                // How many days until Friday?
                var daysTillFriday = 5 - weekDay;
                // Calculate when the next Friday is.
                var rangeBegin =  now + (86400000 * daysTillFriday);
                // Calculate when teh next Sunday is. 
                var rangeEnd = now + (86400000 * (daysTillFriday + 2));
                return dateRange(rangeBegin, rangeEnd);
            }  
            // Is it sunday? Just load today.
            else if(weekDay == 0){
                return oneDay(now);
            } 
            // Its Friday or Saturday.
            else{
                // How many days are left in the weekend?
                var daysLeft = 7 - weekday;
                var rangeEnd =  now + (86400000 * daysLeft);
                // Today is in the weekend, so get form now to rangeEnd
                return dateRange(now, rangeEnd); 
            }

        case "This Week":
            // return events where start or end is between now and now + 7days
            var weekFromNow = now + (86400000 * 7);
            return dateRange(now, weekFromNow);

        case "Next Week":
            // return events where start or end is between now+7 days and now + 14days
            var weekFromNow = now + (86400000 * 7);
            var twoWeeksFromNow = now + (86400000 * 14);
            return dateRange(weekFromNow, twoWeeksFromNow);
            
        case "This Month":
            // return events where the start or end is in the same month as now.
            // Let's first determine what month is is
            var date = new Date(now);
            var month = date.getMonth();
            var isLeapYear = false;
            // Is it leap year?
            if(date.getUTCFullYear() % 4 == 0){
                isLeapYear = true;
            }
            // Let's get the number of days left in the month
            var daysLeft = daysInMonth[month] - date.getUTCDay();
            // If its February and leap year, add one day.
            if(isLeapYear && month ==1){
                daysLeft += 1;
            }
            // If there are less than 5 days left, we are going to do a range from 
            // now to the end of the next month.
            if(daysLeft <= 5){
                var daysTillEndOfNextMonth = dayLeft + daysInMonth[month+1]
                // If the next month is February and its leap year, add one day.
                if(isLeapYear && month+1 == 1){
                    daysTillEndOfNextMonth += 1;
                }
                var rangeEnd = now + (86400000 * daysTillEndOfNextMonth);
            }
            else{
                // More than 5 days left, lets just do from now till the end of this month.
                var rangeEnd = now + (86400000 * daysLeft)
            }
            return dateRange(now, rangeEnd);
        
        default:
            return undefined;
    }

    function oneDay(begin){
        var rangeBegin = new Date(begin).toISOString();
        return [
            [
                {
                    StartUtc: rangeBegin,
                    op: '<='
                },
                'and',
                {
                    EndUtc: rangeBegin,
                    op: '>='
                } 
            ]
        ];
    }

    function dateRange(begin, end){
        var rangeBegin = new Date(begin).toISOString();
        var rangeEnd = new Date(end).toISOString();
        return [
            [
                {
                    StartUtc: rangeBegin,
                    op: '>='
                },
                'and',
                {
                    EndUtc: rangeBegin,
                    op: '<='
                }
            ],
            'or',
            [
                {
                    StartUtc: rangeEnd,
                    op: '>=' 
                },
                'and',
                {
                    EndUtc: rangeEnd,
                    op: '<='
                }
            ]
        ];
    }
} 