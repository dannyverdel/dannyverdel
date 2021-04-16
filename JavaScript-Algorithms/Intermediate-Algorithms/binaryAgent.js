function binaryAgent(str) {
    let outputStr = str.split(' ')
        .map(bin => String.fromCharCode(parseInt(bin, 2))) 
        .join('');
    return outputStr;
  }